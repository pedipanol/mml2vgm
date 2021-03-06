﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class YM2151 : ClsChip
    {
        public YM2151(ClsVgm parent, int chipID, string initialPartName, string stPath,int chipNumber) : base(parent, chipID, initialPartName, stPath, chipNumber)
        {

            _Name = "YM2151";
            _ShortName = "OPM";
            _ChMax = 8;
            _canUsePcm = false;

            Frequency = 3579545;
            port = new byte[][] { new byte[] { (byte)(chipNumber!=0 ? 0xa4 : 0x54) } };

            if (string.IsNullOrEmpty(initialPartName)) return;

            MakeFNumTbl();
            Ch = new ClsChannel[ChMax];
            SetPartToCh(Ch, initialPartName);
            foreach (ClsChannel ch in Ch)
            {
                ch.Type = enmChannelType.FMOPM;
                ch.chipNumber = chipID == 1;
            }

        }

        public override void InitChip()
        {
            if (!use) return;

            //initialize shared param

            //FM Off
            OutAllKeyOff();

            foreach (partWork pw in lstPartWork)
            {
                if (pw.ch == 0)
                {
                    pw.hardLfoFreq = 0;
                    pw.hardLfoPMD = 0;
                    pw.hardLfoAMD = 0;

                    //Reset Hard LFO
                    OutSetHardLfoFreq(null,pw, pw.hardLfoFreq);
                    OutSetHardLfoDepth(null,pw, false, pw.hardLfoAMD);
                    OutSetHardLfoDepth(null,pw, true, pw.hardLfoPMD);
                }

                pw.ams = 0;
                pw.pms = 0;
                if (!pw.dataEnd) OutSetPMSAMS(null,pw, 0, 0);

            }

            if (ChipID!= 0 && parent.info.format != enmFormat.ZGM)
            {
                parent.dat[0x33] = new outDatum(enmMMLType.unknown, null, null, (byte)(parent.dat[0x33].val | 0x40));//use Secondary
            }
        }

        public override void InitPart(partWork pw)
        {
            pw.slots = 0xf;
            pw.volume = 127;
            pw.MaxVolume = 127;
            pw.port = port;
            pw.mixer = 0;
            pw.noise = 0;
        }


        public void OutSetFnum(MML mml, partWork pw, int octave, int note, int kf)
        {
            octave &= 0x7;
            note &= 0xf;
            note = note < 3 ? note : (note < 6 ? (note + 1) : (note < 9 ? (note + 2) : (note + 3)));
            parent.OutData(mml, port[0], (byte)(0x28 + pw.ch), (byte)((octave << 4) | note));
            parent.OutData(mml, port[0], (byte)(0x30 + pw.ch), (byte)(kf << 2));
        }

        public void OutSetVolume(partWork pw, MML mml, int vol, int n)
        {
            if (!parent.instFM.ContainsKey(n))
            {
                msgBox.setWrnMsg(string.Format(msg.get("E16000"), n), mml.line.Lp);
                return;
            }

            int alg = parent.instFM[n][45] & 0x7;
            int[] ope = new int[4] {
                parent.instFM[n][0*Const.INSTRUMENT_M_OPERATOR_SIZE + 6]
                , parent.instFM[n][1 * Const.INSTRUMENT_M_OPERATOR_SIZE + 6]
                , parent.instFM[n][2 * Const.INSTRUMENT_M_OPERATOR_SIZE + 6]
                , parent.instFM[n][3 * Const.INSTRUMENT_M_OPERATOR_SIZE + 6]
            };
            int[][] algs = new int[8][]
            {
                new int[4] { 0,0,0,1}
                ,new int[4] { 0,0,0,1}
                ,new int[4] { 0,0,0,1}
                ,new int[4] { 0,0,0,1}
                ,new int[4] { 0,1,0,1}
                ,new int[4] { 0,1,1,1}
                ,new int[4] { 0,1,1,1}
                ,new int[4] { 1,1,1,1}
            };

            //int minV = 127;
            //for (int i = 0; i < 4; i++)
            //{
            //    if (algs[alg][i] == 1 && (pw.slots & (1 << i)) != 0)
            //    {
            //        minV = Math.Min(minV, ope[i]);
            //    }
            //}

            for (int i = 0; i < 4; i++)
            {
                if (algs[alg][i] == 0 || (pw.slots & (1 << i)) == 0)
                {
                    ope[i] = -1;
                    continue;
                }
                //ope[i] = ope[i] - minV + (127 - vol);
                ope[i] = ope[i] + (127 - vol);
                if (ope[i] < 0)
                {
                    ope[i] = 0;
                }
                if (ope[i] > 127)
                {
                    ope[i] = 127;
                }
            }

            if ((pw.slots & 1) != 0 && ope[0] != -1) OutSetTl(mml,pw, 0, ope[0]);
            if ((pw.slots & 2) != 0 && ope[1] != -1) OutSetTl(mml,pw, 1, ope[1]);
            if ((pw.slots & 4) != 0 && ope[2] != -1) OutSetTl(mml,pw, 2, ope[2]);
            if ((pw.slots & 8) != 0 && ope[3] != -1) OutSetTl(mml,pw, 3, ope[3]);
        }

        public void OutSetTl(MML mml,partWork pw, int ope, int tl)
        {
            ope = (ope == 1) ? 2 : ((ope == 2) ? 1 : ope);
            tl &= 0x7f;

            parent.OutData(
                mml,
                port[0]
                , (byte)(0x60 + pw.ch + ope * 8)
                , (byte)tl
                );
        }

        public void OutSetHardLfoFreq(MML mml,partWork pw, int freq)
        {
            parent.OutData(
                mml,
                port[0]
                , 0x18
                , (byte)(freq & 0xff)
                );
        }

        public void OutSetHardLfoDepth(MML mml,partWork pw, bool isPMD, int depth)
        {
            parent.OutData(
                mml,
                port[0]
                , 0x19
                , (byte)((isPMD ? 0x80 : 0x00) | (depth & 0x7f))
                );
        }

        public void OutSetPMSAMS(MML mml,partWork pw, int PMS, int AMS)
        {
            parent.OutData(
                mml,
                port[0]
                , (byte)(0x38 + pw.ch)
                , (byte)(((PMS & 0x7) << 4) | (AMS & 0x3))
                );
        }

        public void OutSetPanFeedbackAlgorithm(MML mml,partWork pw, int pan, int fb, int alg)
        {
            pan &= 3;
            fb &= 7;
            alg &= 7;

            parent.OutData(mml,port[0], (byte)(0x20 + pw.ch), (byte)((pan << 6) | (fb << 3) | alg));
        }

        public void OutSetDtMl(MML mml,partWork pw, int ope, int dt, int ml)
        {
            ope = (ope == 1) ? 2 : ((ope == 2) ? 1 : ope);
            dt &= 7;
            ml &= 15;

            parent.OutData(mml,port[0], (byte)(0x40 + pw.ch + ope * 8), (byte)((dt << 4) | ml));
        }

        public void OutSetKsAr(MML mml,partWork pw, int ope, int ks, int ar)
        {
            ope = (ope == 1) ? 2 : ((ope == 2) ? 1 : ope);
            ks &= 3;
            ar &= 31;

            parent.OutData(mml,port[0], (byte)(0x80 + pw.ch + ope * 8), (byte)((ks << 6) | ar));
        }

        public void OutSetAmDr(MML mml,partWork pw, int ope, int am, int dr)
        {
            ope = (ope == 1) ? 2 : ((ope == 2) ? 1 : ope);
            am &= 1;
            dr &= 31;

            parent.OutData(mml,port[0], (byte)(0xa0 + pw.ch + ope * 8), (byte)((am << 7) | dr));
        }

        public void OutSetDt2Sr(MML mml,partWork pw, int ope, int dt2, int sr)
        {
            ope = (ope == 1) ? 2 : ((ope == 2) ? 1 : ope);
            dt2 &= 3;
            sr &= 31;

            parent.OutData(mml,port[0], (byte)(0xc0 + pw.ch + ope * 8), (byte)((dt2 << 6) | sr));
        }

        public void OutSetSlRr(MML mml,partWork pw, int ope, int sl, int rr)
        {
            ope = (ope == 1) ? 2 : ((ope == 2) ? 1 : ope);
            sl &= 15;
            rr &= 15;

            parent.OutData(mml,port[0], (byte)(0xe0 + pw.ch + ope * 8), (byte)((sl << 4) | rr));
        }

        public void OutSetHardLfo(MML mml,partWork pw, bool sw, List<int> param)
        {
            if (sw)
            {
                parent.OutData(mml,port[0], 0x1b, (byte)(param[0] & 0x3));//type
                parent.OutData(mml,port[0], 0x18, (byte)(param[1] & 0xff));//LFRQ
                parent.OutData(mml,port[0], 0x19, (byte)((param[2] & 0x7f) | 0x80));//PMD
                parent.OutData(mml,port[0], 0x19, (byte)((param[3] & 0x7f) | 0x00));//AMD
            }
            else
            {
                parent.OutData(mml,port[0], 0x1b, 0);//type
                parent.OutData(mml,port[0], 0x18, 0);//LFRQ
                parent.OutData(mml,port[0], 0x19, 0x80);//PMD
                parent.OutData(mml,port[0], 0x19, 0x00);//AMD
            }
        }

        public void OutSetInstrument(partWork pw, MML mml, int n, int vol, int modeBeforeSend)
        {

            if (!parent.instFM.ContainsKey(n))
            {
                msgBox.setWrnMsg(string.Format(msg.get("E16001"), n), mml.line.Lp);
                return;
            }

            switch (modeBeforeSend)
            {
                case 0: // N)one
                    break;
                case 1: // R)R only
                    for (int ope = 0; ope < 4; ope++) OutSetSlRr(mml, pw, ope, 0, 15);
                    break;
                case 2: // A)ll
                    for (int ope = 0; ope < 4; ope++)
                    {
                        OutSetDtMl(mml, pw, ope, 0, 0);
                        OutSetKsAr(mml, pw, ope, 3, 31);
                        OutSetAmDr(mml, pw, ope, 1, 31);
                        OutSetDt2Sr(mml, pw, ope, 0, 31);
                        OutSetSlRr(mml, pw, ope, 0, 15);
                    }
                    OutSetPanFeedbackAlgorithm(mml, pw, (int)pw.pan.val, 7, 7);
                    break;
            }

            for (int ope = 0; ope < 4; ope++)
            {

                OutSetDtMl(mml, pw, ope, parent.instFM[n][ope * Const.INSTRUMENT_M_OPERATOR_SIZE + 9], parent.instFM[n][ope * Const.INSTRUMENT_M_OPERATOR_SIZE + 8]);
                OutSetKsAr(mml, pw, ope, parent.instFM[n][ope * Const.INSTRUMENT_M_OPERATOR_SIZE + 7], parent.instFM[n][ope * Const.INSTRUMENT_M_OPERATOR_SIZE + 1]);
                OutSetAmDr(mml, pw, ope, parent.instFM[n][ope * Const.INSTRUMENT_M_OPERATOR_SIZE + 11], parent.instFM[n][ope * Const.INSTRUMENT_M_OPERATOR_SIZE + 2]);
                OutSetDt2Sr(mml, pw, ope, parent.instFM[n][ope * Const.INSTRUMENT_M_OPERATOR_SIZE + 10], parent.instFM[n][ope * Const.INSTRUMENT_M_OPERATOR_SIZE + 3]);
                OutSetSlRr(mml, pw, ope, parent.instFM[n][ope * Const.INSTRUMENT_M_OPERATOR_SIZE + 5], parent.instFM[n][ope * Const.INSTRUMENT_M_OPERATOR_SIZE + 4]);

            }
            pw.op1ml = parent.instFM[n][0 * Const.INSTRUMENT_M_OPERATOR_SIZE + 8];
            pw.op2ml = parent.instFM[n][1 * Const.INSTRUMENT_M_OPERATOR_SIZE + 8];
            pw.op3ml = parent.instFM[n][2 * Const.INSTRUMENT_M_OPERATOR_SIZE + 8];
            pw.op4ml = parent.instFM[n][3 * Const.INSTRUMENT_M_OPERATOR_SIZE + 8];
            pw.op1dt2 = parent.instFM[n][0 * Const.INSTRUMENT_M_OPERATOR_SIZE + 10];
            pw.op2dt2 = parent.instFM[n][1 * Const.INSTRUMENT_M_OPERATOR_SIZE + 10];
            pw.op3dt2 = parent.instFM[n][2 * Const.INSTRUMENT_M_OPERATOR_SIZE + 10];
            pw.op4dt2 = parent.instFM[n][3 * Const.INSTRUMENT_M_OPERATOR_SIZE + 10];

            OutSetPanFeedbackAlgorithm(mml, pw, (int)pw.pan.val, parent.instFM[n][46], parent.instFM[n][45]);

            int alg = parent.instFM[n][45] & 0x7;
            int[] op = new int[4] {
                parent.instFM[n][0*Const.INSTRUMENT_M_OPERATOR_SIZE + 6]
                , parent.instFM[n][1 * Const.INSTRUMENT_M_OPERATOR_SIZE + 6]
                , parent.instFM[n][2 * Const.INSTRUMENT_M_OPERATOR_SIZE + 6]
                , parent.instFM[n][3 * Const.INSTRUMENT_M_OPERATOR_SIZE + 6]
            };
            int[][] algs = new int[8][]
            {
                new int[4] { 1,1,1,0}
                ,new int[4] { 1,1,1,0}
                ,new int[4] { 1,1,1,0}
                ,new int[4] { 1,1,1,0}
                ,new int[4] { 1,0,1,0}
                ,new int[4] { 1,0,0,0}
                ,new int[4] { 1,0,0,0}
                ,new int[4] { 0,0,0,0}
            };

            for (int i = 0; i < 4; i++)
            {
                if (algs[alg][i] == 0 || (pw.slots & (1 << i)) == 0)
                {
                    op[i] = -1;
                    continue;
                }
                if (op[i] < 0)
                {
                    op[i] = 0;
                }
                if (op[i] > 127)
                {
                    op[i] = 127;
                }
            }

            if ((pw.slots & 1) != 0 && op[0] != -1) OutSetTl(mml, pw, 0, op[0]);
            if ((pw.slots & 2) != 0 && op[1] != -1) OutSetTl(mml, pw, 1, op[1]);
            if ((pw.slots & 4) != 0 && op[2] != -1) OutSetTl(mml, pw, 2, op[2]);
            if ((pw.slots & 8) != 0 && op[3] != -1) OutSetTl(mml, pw, 3, op[3]);

            ((YM2151)pw.chip).OutSetVolume(pw, mml, vol, n);

        }

        public void OutKeyOn(MML mml,partWork pw)
        {

            if (pw.ch == 7 && pw.mixer == 1)
            {
                parent.OutData(mml,port[0], 0x0f, (byte)((pw.mixer << 7) | (pw.noise & 0x1f)));
            }
            //key on
            parent.OutData(mml,port[0], 0x08, (byte)((pw.slots << 3) + pw.ch));
        }

        public void OutKeyOff(MML mml,partWork pw)
        {

            //key off
            parent.OutData(mml,port[0], 0x08, (byte)(0x00 + (pw.ch & 7)));
            if (pw.ch == 7 && pw.mixer == 1)
            {
                parent.OutData(mml,port[0], 0x0f, 0x00);
            }

        }

        public void OutAllKeyOff()
        {

            foreach (partWork pw in lstPartWork)
            {
                if (pw.dataEnd) continue;

                OutKeyOff(null,pw);
                OutSetTl(null,pw, 0, 127);
                OutSetTl(null,pw, 1, 127);
                OutSetTl(null,pw, 2, 127);
                OutSetTl(null,pw, 3, 127);
            }

        }


        public override void SetFNum(partWork pw, MML mml)
        {

            int f = GetFNum(pw,mml,pw.octaveNow, pw.noteCmd, pw.shift + pw.keyShift + pw.toneDoublerKeyShift);//

            if (pw.bendWaitCounter != -1)
            {
                f = pw.bendFnum;
            }

            f = f + pw.detune;
            for (int lfo = 0; lfo < 4; lfo++)
            {
                if (!pw.lfo[lfo].sw)
                {
                    continue;
                }
                if (pw.lfo[lfo].type != eLfoType.Vibrato)
                {
                    continue;
                }
                f += pw.lfo[lfo].value + pw.lfo[lfo].param[6];
            }

            f = Common.CheckRange(f, 0, 9 * 12 * 64 - 1);
            int oct = f / (12 * 64);
            int note = (f - oct * 12 * 64) / 64;
            int kf = f - oct * 12 * 64 - note * 64;

            OutSetFnum(mml,pw, oct, note, kf);
        }

        public override int GetFNum(partWork pw, MML mml, int octave, char noteCmd, int shift)
        {
            int o = octave;
            int n = Const.NOTE.IndexOf(noteCmd) + shift - 1;

            o += n / 12;
            n %= 12;
            if (n < 0)
            {
                n += 12;
                o = Common.CheckRange(--o, 1, 8);
            }
            //if (n >= 0)
            //{
            //    o += n / 12;
            //    o = Common.CheckRange(o, 1, 8);
            //    n %= 12;
            //}
            //else
            //{
            //    o += n / 12 - ((n % 12 == 0) ? 0 : 1);
            //    if (o == 0 && n < 0)
            //    {
            //        o = 1;
            //        n = 0;
            //    }
            //    else
            //    {
            //        o = Common.CheckRange(o, 1, 8);
            //        n %= 12;
            //        if (n < 0) { n += 12; }
            //    }
            //}
            o--;

            return n * 64 + o * 12 * 64;
        }

        public override void SetVolume(partWork pw, MML mml)
        {
            int vol = pw.volume;

            for (int lfo = 0; lfo < 4; lfo++)
            {
                if (!pw.lfo[lfo].sw)
                {
                    continue;
                }
                if (pw.lfo[lfo].type != eLfoType.Tremolo)
                {
                    continue;
                }
                vol += pw.lfo[lfo].value + pw.lfo[lfo].param[6];
            }

            if (pw.beforeVolume != vol)
            {
                if (parent.instFM.ContainsKey(pw.instrument))
                {
                    OutSetVolume(pw,mml, vol, pw.instrument);
                    pw.beforeVolume = vol;
                }
            }
        }

        public override void SetKeyOn(partWork pw, MML mml)
        {
            OutKeyOn(mml,pw);
        }

        public override void SetKeyOff(partWork pw, MML mml)
        {
            OutKeyOff(mml,pw);
        }

        public override void SetLfoAtKeyOn(partWork pw, MML mml)
        {
            for (int lfo = 0; lfo < 4; lfo++)
            {
                clsLfo pl = pw.lfo[lfo];

                if (!pl.sw)
                    continue;
                if (pl.type == eLfoType.Hardware)
                    continue;
                if (pl.param[5] != 1)
                    continue;

                pl.isEnd = false;
                pl.value = (pl.param[0] == 0) ? pl.param[6] : 0;//ディレイ中は振幅補正は適用されない
                pl.waitCounter = pl.param[0];
                pl.direction = pl.param[2] < 0 ? -1 : 1;
                pl.depthWaitCounter = pl.param[7];
                pl.depth = pl.param[3];
                pl.depthV2 = pl.param[2];

                if (pl.type == eLfoType.Vibrato)
                    SetFNum(pw,mml);

                if (pl.type == eLfoType.Tremolo)
                    SetVolume(pw,mml);

            }
        }

        public override int GetToneDoublerShift(partWork pw, int octave, char noteCmd, int shift)
        {
            int i = pw.instrument;
            if (pw.TdA == -1)
            {
                return 0;
            }

            int TdB = octave * 12 + Const.NOTE.IndexOf(noteCmd) + shift;
            int s = pw.TdA - TdB;
            int us = Math.Abs(s);
            int n = pw.toneDoubler;
            if (us >= parent.instToneDoubler[n].lstTD.Count)
            {
                return 0;
            }

            return ((s < 0) ? s : 0) + parent.instToneDoubler[n].lstTD[us].KeyShift;
        }

        public override void SetToneDoubler(partWork pw,MML mml)
        {
            int i = pw.instrument;
            if (i < 0) return;

            pw.toneDoublerKeyShift = 0;
            byte[] instFM = parent.instFM[i];
            if (instFM == null || instFM.Length < 1) return;
            Note note = (Note)mml.args[0];

            if (pw.TdA == -1)
            {
                //resetToneDoubler
                //ML
                if (pw.op1ml != instFM[0 * Const.INSTRUMENT_M_OPERATOR_SIZE + 8])
                {
                    ((YM2151)pw.chip).OutSetDtMl(mml,pw, 0, instFM[0 * Const.INSTRUMENT_M_OPERATOR_SIZE + 9], instFM[0 * Const.INSTRUMENT_M_OPERATOR_SIZE + 8]);
                    pw.op1ml = instFM[0 * Const.INSTRUMENT_M_OPERATOR_SIZE + 8];
                }
                if (pw.op2ml != instFM[1 * Const.INSTRUMENT_M_OPERATOR_SIZE + 8])
                {
                    ((YM2151)pw.chip).OutSetDtMl(mml, pw, 1, instFM[1 * Const.INSTRUMENT_M_OPERATOR_SIZE + 9], instFM[1 * Const.INSTRUMENT_M_OPERATOR_SIZE + 8]);
                    pw.op2ml = instFM[1 * Const.INSTRUMENT_M_OPERATOR_SIZE + 8];
                }
                if (pw.op3ml != instFM[2 * Const.INSTRUMENT_M_OPERATOR_SIZE + 8])
                {
                    ((YM2151)pw.chip).OutSetDtMl(mml, pw, 2, instFM[2 * Const.INSTRUMENT_M_OPERATOR_SIZE + 9], instFM[2 * Const.INSTRUMENT_M_OPERATOR_SIZE + 8]);
                    pw.op3ml = instFM[2 * Const.INSTRUMENT_M_OPERATOR_SIZE + 8];
                }
                if (pw.op4ml != instFM[3 * Const.INSTRUMENT_M_OPERATOR_SIZE + 8])
                {
                    ((YM2151)pw.chip).OutSetDtMl(mml, pw, 3, instFM[3 * Const.INSTRUMENT_M_OPERATOR_SIZE + 9], instFM[3 * Const.INSTRUMENT_M_OPERATOR_SIZE + 8]);
                    pw.op4ml = instFM[3 * Const.INSTRUMENT_M_OPERATOR_SIZE + 8];
                }
                //DT2
                if (pw.op1dt2 != instFM[0 * Const.INSTRUMENT_M_OPERATOR_SIZE + 10])
                {
                    ((YM2151)pw.chip).OutSetDt2Sr(mml, pw, 0, instFM[0 * Const.INSTRUMENT_M_OPERATOR_SIZE + 10], instFM[0 * Const.INSTRUMENT_M_OPERATOR_SIZE + 3]);
                    pw.op1dt2 = instFM[0 * Const.INSTRUMENT_M_OPERATOR_SIZE + 10];
                }
                if (pw.op2dt2 != instFM[1 * Const.INSTRUMENT_M_OPERATOR_SIZE + 10])
                {
                    ((YM2151)pw.chip).OutSetDt2Sr(mml, pw, 1, instFM[1 * Const.INSTRUMENT_M_OPERATOR_SIZE + 10], instFM[1 * Const.INSTRUMENT_M_OPERATOR_SIZE + 3]);
                    pw.op2dt2 = instFM[1 * Const.INSTRUMENT_M_OPERATOR_SIZE + 10];
                }
                if (pw.op3dt2 != instFM[2 * Const.INSTRUMENT_M_OPERATOR_SIZE + 10])
                {
                    ((YM2151)pw.chip).OutSetDt2Sr(mml, pw, 2, instFM[2 * Const.INSTRUMENT_M_OPERATOR_SIZE + 10], instFM[2 * Const.INSTRUMENT_M_OPERATOR_SIZE + 3]);
                    pw.op3dt2 = instFM[2 * Const.INSTRUMENT_M_OPERATOR_SIZE + 10];
                }
                if (pw.op4dt2 != instFM[3 * Const.INSTRUMENT_M_OPERATOR_SIZE + 10])
                {
                    ((YM2151)pw.chip).OutSetDt2Sr(mml, pw, 3, instFM[3 * Const.INSTRUMENT_M_OPERATOR_SIZE + 10], instFM[3 * Const.INSTRUMENT_M_OPERATOR_SIZE + 3]);
                    pw.op4dt2 = instFM[3 * Const.INSTRUMENT_M_OPERATOR_SIZE + 10];
                }
            }
            else
            {
                //setToneDoubler
                int oct = pw.octaveNow;
                foreach(MML octMml in note.tDblOctave)
                {
                    switch (octMml.type)
                    {
                        case enmMMLType.Octave:
                            oct = (int)octMml.args[0];
                            break;
                        case enmMMLType.OctaveUp:
                            oct++;
                            break;
                        case enmMMLType.OctaveDown:
                            oct--;
                            break;
                    }
                }
                oct = Common.CheckRange(oct, 1, 8);
                pw.octaveNew = oct;
                int TdB = oct * 12 + Const.NOTE.IndexOf(note.tDblCmd) + note.tDblShift + pw.keyShift;
                int s = TdB - pw.TdA;// - TdB;
                int us = Math.Abs(s);
                int n = pw.toneDoubler;
                clsToneDoubler instToneDoubler = parent.instToneDoubler[n];
                if (us >= instToneDoubler.lstTD.Count)
                {
                    return;
                }

                pw.toneDoublerKeyShift = ((s < 0) ? s : 0) + instToneDoubler.lstTD[us].KeyShift;

                //ML
                if (pw.op1ml != instToneDoubler.lstTD[us].OP1ML)
                {
                    ((YM2151)pw.chip).OutSetDtMl(mml,pw, 0, instFM[0 * Const.INSTRUMENT_M_OPERATOR_SIZE + 9], instToneDoubler.lstTD[us].OP1ML);
                    pw.op1ml = instToneDoubler.lstTD[us].OP1ML;
                }
                if (pw.op2ml != instToneDoubler.lstTD[us].OP2ML)
                {
                    ((YM2151)pw.chip).OutSetDtMl(mml, pw, 1, instFM[1 * Const.INSTRUMENT_M_OPERATOR_SIZE + 9], instToneDoubler.lstTD[us].OP2ML);
                    pw.op2ml = instToneDoubler.lstTD[us].OP2ML;
                }
                if (pw.op3ml != instToneDoubler.lstTD[us].OP3ML)
                {
                    ((YM2151)pw.chip).OutSetDtMl(mml, pw, 2, instFM[2 * Const.INSTRUMENT_M_OPERATOR_SIZE + 9], instToneDoubler.lstTD[us].OP3ML);
                    pw.op3ml = instToneDoubler.lstTD[us].OP3ML;
                }
                if (pw.op4ml != instToneDoubler.lstTD[us].OP4ML)
                {
                    ((YM2151)pw.chip).OutSetDtMl(mml, pw, 3, instFM[3 * Const.INSTRUMENT_M_OPERATOR_SIZE + 9], instToneDoubler.lstTD[us].OP4ML);
                    pw.op4ml = instToneDoubler.lstTD[us].OP4ML;
                }
                //DT2
                if (pw.op1dt2 != instToneDoubler.lstTD[us].OP1DT2)
                {
                    ((YM2151)pw.chip).OutSetDt2Sr(mml, pw, 0, instToneDoubler.lstTD[us].OP1DT2, instFM[0 * Const.INSTRUMENT_M_OPERATOR_SIZE + 3]);
                    pw.op1dt2 = instToneDoubler.lstTD[us].OP1DT2;
                }
                if (pw.op2dt2 != instToneDoubler.lstTD[us].OP2DT2)
                {
                    ((YM2151)pw.chip).OutSetDt2Sr(mml, pw, 1, instToneDoubler.lstTD[us].OP2DT2, instFM[1 * Const.INSTRUMENT_M_OPERATOR_SIZE + 3]);
                    pw.op2dt2 = instToneDoubler.lstTD[us].OP2DT2;
                }
                if (pw.op3dt2 != instToneDoubler.lstTD[us].OP3DT2)
                {
                    ((YM2151)pw.chip).OutSetDt2Sr(mml, pw, 2, instToneDoubler.lstTD[us].OP3DT2, instFM[2 * Const.INSTRUMENT_M_OPERATOR_SIZE + 3]);
                    pw.op3dt2 = instToneDoubler.lstTD[us].OP3DT2;
                }
                if (pw.op4dt2 != instToneDoubler.lstTD[us].OP4DT2)
                {
                    ((YM2151)pw.chip).OutSetDt2Sr(mml, pw, 3, instToneDoubler.lstTD[us].OP4DT2, instFM[3 * Const.INSTRUMENT_M_OPERATOR_SIZE + 3]);
                    pw.op4dt2 = instToneDoubler.lstTD[us].OP4DT2;
                }

                //pw.TdA = -1;
            }
        }


        public override void CmdNoiseToneMixer(partWork pw, MML mml)
        {
            int n = (int)mml.args[0];
            n = Common.CheckRange(n, 0, 1);
            pw.mixer = n;
        }

        public override void CmdNoise(partWork pw, MML mml)
        {
            int n = (int)mml.args[0];
            n = Common.CheckRange(n, 0, 31);
            if (pw.noise != n)
            {
                pw.noise = n;
            }
        }

        public override void CmdMPMS(partWork pw, MML mml)
        {
            int n = (int)mml.args[1];
            n = Common.CheckRange(n, 0, 7);
            pw.pms = n;
            ((YM2151)pw.chip).OutSetPMSAMS(mml,pw, pw.pms, pw.ams);
        }

        public override void CmdMAMS(partWork pw, MML mml)
        {
            int n = (int)mml.args[1];
            n = Common.CheckRange(n, 0, 3);
            pw.ams = n;
            ((YM2151)pw.chip).OutSetPMSAMS(mml,pw, pw.pms, pw.ams);
        }

        public override void CmdLfo(partWork pw, MML mml)
        {
            base.CmdLfo(pw, mml);

            if (mml.args[0] is string) return;

            int c = (char)mml.args[0] - 'P';
            if (pw.lfo[c].type == eLfoType.Hardware)
            {
                if (pw.lfo[c].param.Count < 4)
                {
                    msgBox.setErrMsg(msg.get("E16002"), mml.line.Lp);
                    return;
                }
                if (pw.lfo[c].param.Count > 5)
                {
                    msgBox.setErrMsg(msg.get("E16003"), mml.line.Lp);
                    return;
                }

                pw.lfo[c].param[0] = Common.CheckRange(pw.lfo[c].param[0], 0, 3); //Type
                pw.lfo[c].param[1] = Common.CheckRange(pw.lfo[c].param[1], 0, 255); //LFRQ
                pw.lfo[c].param[2] = Common.CheckRange(pw.lfo[c].param[2], 0, 127); //PMD
                pw.lfo[c].param[3] = Common.CheckRange(pw.lfo[c].param[3], 0, 127); //AMD
                if (pw.lfo[c].param.Count == 5)
                {
                    pw.lfo[c].param[4] = Common.CheckRange(pw.lfo[c].param[4], 0, 1);
                }
                else
                {
                    pw.lfo[c].param.Add(0);
                }
            }
        }

        public override void CmdLfoSwitch(partWork pw, MML mml)
        {
            base.CmdLfoSwitch(pw, mml);

            int c = (char)mml.args[0] - 'P';
            int n = (int)mml.args[1];
            if (pw.lfo[c].type == eLfoType.Hardware)
            {
                ((YM2151)pw.chip).OutSetHardLfo(mml,pw, (n == 0) ? false : true, pw.lfo[c].param);
            }
        }

        public override void CmdPan(partWork pw, MML mml)
        {
            int n = (int)mml.args[0];
            n = Common.CheckRange(n, 0, 3);
            pw.pan.val = (n == 1) ? 2 : (n == 2 ? 1 : n);
            if (pw.instrument < 0)
            {
                msgBox.setErrMsg(msg.get("E16004")
                    , mml.line.Lp);
            }
            else
            {
                ((YM2151)pw.chip).OutSetPanFeedbackAlgorithm(
                    mml,
                    pw
                    , (int)pw.pan.val
                    , parent.instFM[pw.instrument][46]
                    , parent.instFM[pw.instrument][45]
                    );
            }
        }

        public override void CmdInstrument(partWork pw, MML mml)
        {
            char type = (char)mml.args[0];
            int n = (int)mml.args[1];

            if (type == 'I')
            {
                msgBox.setErrMsg(msg.get("E16005"), mml.line.Lp);
                return;
            }

            if (type == 'T')
            {
                n = Common.CheckRange(n, 0, 255);
                pw.toneDoubler = n;
                return;
            }

            if (type == 'E')
            {
                SetEnvelopParamFromInstrument(pw, n,mml);
                return;
            }

            n = Common.CheckRange(n, 0, 255);
            if (pw.instrument == n) return;

            pw.instrument = n;
            int modeBeforeSend = parent.info.modeBeforeSend;
            if (type == 'N')
            {
                modeBeforeSend = 0;
            }
            else if (type == 'R')
            {
                modeBeforeSend = 1;
            }
            else if (type == 'A')
            {
                modeBeforeSend = 2;
            }

            OutSetInstrument(pw,mml, n, pw.volume, modeBeforeSend);
        }

        public override void CmdY(partWork pw, MML mml)
        {
            if (mml.args[0] is string toneparamName)
            {
                byte op = (byte)mml.args[1];
                op = (byte)(op == 1 ? 2 : (op == 2 ? 1 : op));
                byte dat = (byte)mml.args[2];

                switch (toneparamName)
                {
                    case "PANFBAL":
                    case "PANFLCON":
                        parent.OutData(mml,port[0], (byte)(0x20 + pw.ch), dat);
                        break;
                    case "PMSAMS":
                        parent.OutData(mml, port[0], (byte)(0x38 + pw.ch), dat);
                        break;
                    case "DTML":
                    case "DTMUL":
                    case "DT1ML":
                    case "DT1MUL":
                        parent.OutData(mml, port[0], (byte)(0x40 + pw.ch + op * 8), dat);
                        break;
                    case "TL":
                        parent.OutData(mml, port[0], (byte)(0x60 + pw.ch + op * 8), dat);
                        break;
                    case "KSAR":
                        parent.OutData(mml, port[0], (byte)(0x80 + pw.ch + op * 8), dat);
                        break;
                    case "AMDR":
                    case "AMED1R":
                        parent.OutData(mml, port[0], (byte)(0xa0 + pw.ch + op * 8), dat);
                        break;
                    case "DT2SR":
                    case "DT2D2R":
                        parent.OutData(mml, port[0], (byte)(0xc0 + pw.ch + op * 8), dat);
                        break;
                    case "SLRR":
                    case "D1LRR":
                        parent.OutData(mml, port[0], (byte)(0xe0 + pw.ch + op * 8), dat);
                        break;
                }
            }
            else
            {
                byte adr = (byte)mml.args[0];
                byte dat = (byte)mml.args[1];
                parent.OutData(mml, port[0], adr, dat);
            }
        }

        public override void CmdLoopExtProc(partWork pw, MML mml)
        {
        }

    }
}
