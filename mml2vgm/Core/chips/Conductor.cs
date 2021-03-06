﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class Conductor : ClsChip
    {
        public Conductor(ClsVgm parent, int chipID, string initialPartName, string stPath, int chipNumber) : base(parent, chipID, initialPartName, stPath, chipNumber)
        {
            _chipType = enmChipType.CONDUCTOR;
            _Name = "CONDUCTOR";
            _ShortName = "CON";
            _ChMax = 2;
            _canUsePcm = false;
            _canUsePI = false;
            ChipNumber = chipNumber;

            Frequency = 44100;
            port = new byte[][]{
                new byte[] { 0x00 }
            };

            if (string.IsNullOrEmpty(initialPartName)) return;

            Ch = new ClsChannel[ChMax];
            SetPartToCh(Ch, initialPartName);
            foreach (ClsChannel ch in Ch)
            {
                ch.Type = enmChannelType.SSG;
                ch.chipNumber = chipID == 1;
                ch.MaxVolume = 15;
            }

        }

        public override void InitPart(partWork pw)
        {
            pw.volume = 15;
            pw.MaxVolume = 15;
            pw.port = port;
        }

        public override void InitChip()
        {
            if (!use) return;

            for (int ch = 0; ch < ChMax; ch++)
            {
                lstPartWork[ch].volume = 0;
            }

        }

        public override void StorePcmRawData(clsPcmDatSeq pds, byte[] buf, bool isRaw, bool is16bit, int samplerate, params object[] option)
        {
        }

        public override bool StorePcmCheck()
        {
            return false;
        }

        public override int GetToneDoublerShift(partWork pw, int octave, char noteCmd, int shift)
        {
            return 0;
        }

        public override void SetToneDoubler(partWork pw, MML mml)
        {
        }

        public override int GetFNum(partWork pw, MML mml, int octave, char cmd, int shift)
        {
            return 0;
        }


        public override void SetFNum(partWork pw, MML mml)
        {
        }

        public override void SetKeyOn(partWork pw, MML mml)
        {
        }

        public override void SetKeyOff(partWork pw, MML mml)
        {
        }

        public override void SetVolume(partWork pw, MML mml)
        {
        }

        public override void SetLfoAtKeyOn(partWork pw, MML mml)
        {
        }

        public override void CmdY(partWork pw, MML mml)
        {
        }

        public override void CmdLoopExtProc(partWork p, MML mml)
        {
        }

        public override void CmdInstrument(partWork pw, MML mml)
        {
        }

    }
}
