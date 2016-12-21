﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mml2vgm
{
    // https://wiki.neogeodev.org/index.php?title=ADPCM_codecs

    public class EncAdpcmA
    {
        static short[] step_size = {
               16, 17, 19, 21, 23, 25, 28, 31, 34, 37,
               41, 45, 50, 55, 60, 66, 73, 80, 88, 97,
               107, 118, 130, 143, 157, 173, 190, 209, 230, 253,
               279, 307, 337, 371, 408, 449, 494, 544, 598, 658,
               724, 796, 876, 963, 1060, 1166, 1282, 1411, 1552
               }; //49 items
        static int[] step_adj = { -1, -1, -1, -1, 2, 5, 7, 9, -1, -1, -1, -1, 2, 5, 7, 9 };

        //buffers
        private short[] inBuffer;   //temp work buffer, used correct byte order and downsample
        private byte[] outBuffer;   //output buffer, this is your PCM file, save it

        //decode stuff
        private int[] jedi_table;
        int acc = 0; //ADPCM accumulator, initial condition must be 0
        int decstep = 0; //ADPCM decoding step, initial condition must be 0

        //encode stuff
        int diff;
        int step;
        int predsample;
        int index;
        int prevsample = 0; // previous sample, initial condition must be 0
        int previndex = 0; //previous index, initial condition must be 0

        //jedi table is used speed up decoding, run this to init the table before encoding. Mame copy-pasta.
        private void jedi_table_init()
        {
            int step, nib;

            jedi_table = new int[16 * 49];
            for (step = 0; step < 49; step++)
            {
                for (nib = 0; nib < 16; nib++)
                {
                    int value = (2 * (nib & 0x07) + 1) * step_size[step] / 8;
                    jedi_table[step * 16 + nib] = ((nib & 0x08) != 0) ? -value : value;
                }
            }
        }

        //decode sub, returns decoded 12bit data
        private short YM2610_ADPCM_A_Decode(byte code)
        {
            acc += jedi_table[decstep + code];
            if ((acc & ~0x7ff) != 0) // acc is > 2047
                acc |= ~0xfff;
            else acc &= 0xfff;
            decstep += step_adj[code & 7] * 16;
            if (decstep < 0) decstep = 0;
            if (decstep > 48 * 16) decstep = 48 * 16;
            return (short)acc;
        }

        // our encoding sub, returns ADPCM nibble
        private byte YM2610_ADPCM_A_Encode(short sample)
        {
            int tempstep;
            byte code;

            predsample = prevsample;
            index = previndex;
            step = step_size[index];

            diff = sample - predsample;
            if (diff >= 0)
                code = 0;
            else
            {
                code = 8;
                diff = -diff;
            }

            tempstep = step;
            if (diff >= tempstep)
            {
                code |= 4;
                diff -= tempstep;
            }
            tempstep >>= 1;
            if (diff >= tempstep)
            {
                code |= 2;
                diff -= tempstep;
            }
            tempstep >>= 1;
            if (diff >= tempstep) code |= 1;

            predsample = YM2610_ADPCM_A_Decode(code);

            index += step_adj[code];
            if (index < 0) index = 0;
            if (index > 48) index = 48;

            prevsample = predsample;
            previndex = index;

            return code;
        }

        public EncAdpcmA()
        {
            jedi_table_init();
        }

        //our main sub, init buffers and runs the encode process
        //enter this with your sound file loaded into buffer
        public byte[] YM_encode(byte[] buffer,bool is16bit)  //input buffer, load your sound file into this
        {
            int i;

            //reset to initial conditions
            acc = 0;
            decstep = 0;
            prevsample = 0;
            previndex = 0;

            if (is16bit)
            {
                //watch out for odd data count & allocate buffers
                if ((buffer.Length / 2) % 2 != 0)
                {
                    inBuffer = new short[(buffer.Length / 2) + 1];
                    inBuffer[inBuffer.Length - 1] = 0x00;
                }
                else inBuffer = new short[buffer.Length / 2];

                //fix byte order and downscale data to 12 bits
                for (i = 0; i < buffer.Length; i += 2)
                {
                    inBuffer[i / 2] = (short)((buffer[i]) | (buffer[i + 1] << 8));
                    inBuffer[i / 2] >>= 4;
                }
            }
            else
            {
                if (buffer.Length % 2 != 0)
                {
                    inBuffer = new short[buffer.Length + 1];
                    inBuffer[inBuffer.Length - 1] = 0x00;
                }
                else inBuffer = new short[buffer.Length];

                for (i = 0; i < buffer.Length; i ++)
                {
                    inBuffer[i] = (short)(buffer[i]);
                    inBuffer[i] <<= 4;
                }
            }

            int outSize = inBuffer.Length / 2;
            outSize = (outSize % 0x100) != 0 ? ((outSize / 0x100) + 1) * 0x100 : outSize;
            outBuffer = new byte[outSize];

            //actual encoding
            for (i = 0; i < inBuffer.Length; i += 2)
            {
                outBuffer[i / 2] = (byte)((YM2610_ADPCM_A_Encode(inBuffer[i]) << 4) | YM2610_ADPCM_A_Encode(inBuffer[i + 1]));
            }
            //padding
            for (i = i/2; i < outBuffer.Length; i ++)
            {
                outBuffer[i] = (byte)((YM2610_ADPCM_A_Encode(0x00) << 4) | YM2610_ADPCM_A_Encode(0x00));
            }

            return outBuffer;
        }
    }
}