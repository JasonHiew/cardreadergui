using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CardReaderGui
{
    public class EMVCard : ISmartCard
    {
        private CardNative cardUpdater;

        private byte[] m_PSEAID = new byte[] { 0x31, 0x50, 0x41, 0x59, 0x2E, 0x53, 0x59, 0x53, 0x2E, 0x44, 0x44, 0x46, 0x30, 0x31 };
        private byte[] m_EMVAID;

        //hard coded AID list in case card does not support PSE
        private byte[][] aidList = new byte[][]{
        new byte[] { 0xA0, 0x00, 0x00, 0x00, 0x03, 0x10, 0x10 },
        new byte[] { 0xA0, 0x00, 0x00, 0x00, 0x03, 0x20, 0x10 },
        new byte[] { 0xA0, 0x00, 0x00, 0x00, 0x04, 0x10, 0x10 },
        new byte[] { 0xA0, 0x00, 0x00, 0x00, 0x04, 0x20, 0x10 },
        new byte[] { 0xA0, 0x00, 0x00, 0x00, 0x42, 0x10, 0x10 },
        new byte[] { 0xA0, 0x00, 0x00, 0x00, 0x42, 0x20, 0x10 },
        new byte[] { 0xA0, 0x00, 0x00, 0x00, 0x69, 0x00 },
        new byte[] { 0xa0, 0x00, 0x00, 0x00, 0x03, 0x10, 0x10, 0x01 },
        new byte[] { 0xa0, 0x00, 0x00, 0x00, 0x03, 0x10, 0x10, 0x02 },
        new byte[] { 0xA0, 0x00, 0x00, 0x00, 0x03, 0x30, 0x10 },
        new byte[] { 0xA0, 0x00, 0x00, 0x00, 0x03, 0x80, 0x10 },
        new byte[] { 0xA0, 0x00, 0x00, 0x00, 0x03, 0x80, 0x10 },
        new byte[] { 0xa0,0x00,0x00,0x00,0x03,0x99,0x99,0x10 },
        new byte[] { 0xa0,0x00,0x00,0x00,0x04,0x30,0x60 },
        new byte[] { 0xa0,0x00,0x00,0x00,0x05,0x00,0x01 },
        new byte[] { 0xa0,0x00,0x00,0x00,0x24,0x01 },
        new byte[] { 0xa0,0x00,0x00,0x00,0x25 },
        new byte[] { 0xa0,0x00,0x00,0x00,0x25,0x01,0x07,0x01 },
        new byte[] { 0xa0,0x00,0x00,0x00,0x29,0x10,0x10 },
        };

        //sfi -> record numbers, e.g. 1 -> 1, 2, 3
        private List<SFIRecords> sfiRecords = new List<SFIRecords>();
        private class SFIRecords
        {
            public byte sfi;
            public byte[] records;
            public SFIRecords(byte sfi, byte[] records) { this.sfi = sfi; this.records = records; }
        }

        public string Label { get; set; }
        public string CardType { get; set; }    //mastercard or visacredit
        public string Name { get; set; }
        public long Number { get; set; }
        public string NumberString { get; set; }
        public DateTime Expiry { get; set; }
        public string ExpiryString { get; set; }
        public IList<object> Properties { get; set; }

        public EMVCard(CardNative cardUpdater)
        {
            this.cardUpdater = cardUpdater;
            this.Properties = new List<object>();
        }

        public bool SelectApplication()
        {
            //select the PSE application
            APDUCommand apduSelectPSEApl = new APDUCommand(0x00, 0xA4, 4, 0, m_PSEAID, 0);
            APDUResponse apduPSE = this.cardUpdater.Transmit(apduSelectPSEApl);
            if (apduPSE.SW1 == 0x90)    //PSE supported
            {
                //send the Read Record command, record 1, SFI 1
                //P2 format:  MSB                  LSB
                //            7   6   5   4   3   2   1   0
                //            SFI SFI SFI SFI SFI 1   0   0
                //1 0 0 in the LSB bits means that P1 is a record number
                APDUCommand apduReadRecord = new APDUCommand(0x00, 0xB2, 1, 0xc, null, 0);
                APDUResponse apdu1 = this.cardUpdater.Transmit(apduReadRecord);
                if (apdu1.SW1 == 0x90)
                {
                    m_EMVAID = new byte[apdu1.Data[5]];
                    Array.Copy(apdu1.Data, 6, m_EMVAID, 0, m_EMVAID.Length);

                    APDUCommand apduSelectEMVApl = new APDUCommand(0x00, 0xA4, 4, 0, m_EMVAID, 0);
                    APDUResponse apdu2 = this.cardUpdater.Transmit(apduSelectEMVApl);
                    if (apdu2.SW1 == 0x90)
                    {
                        //Label = ASCIIEncoding.ASCII.GetString(apdu2.Data, 15, apdu2.Data[14]);
                        if (apdu2.Data[0] == 0x6f)  //fci template
                        {
                            ExtractData(ReadTagData(apdu2.Data, 0));
                        }
                        return true;
                    }
                }
            }

            //PSE unsupported, need to try AID list
            foreach (byte[] aid in aidList)
            {
                APDUCommand apduSelectEMVApl = new APDUCommand(0x00, 0xA4, 4, 0, aid, 0);
                APDUResponse apdu2 = this.cardUpdater.Transmit(apduSelectEMVApl);
                if (apdu2.SW1 == 0x90)
                {
                    //Label = ASCIIEncoding.ASCII.GetString(apdu2.Data, 15, apdu2.Data[14]);
                    //found it!
                    m_EMVAID = aid;
                    if (apdu2.Data[0] == 0x6f)  //fci template
                    {
                        ExtractData(ReadTagData(apdu2.Data, 0));
                    }
                    return true;
                }
            }

            return false;
        }

        public void ReadCard()
        {
            //get processing options
            ReadGPO();

            //read all possible data and extract needed  info
            foreach (SFIRecords sfir in sfiRecords)
            {
                byte p2 = (byte)((sfir.sfi << 3) | 4);
                foreach (byte p1 in sfir.records)
                {
                    //read each record
                    APDUCommand apduReadRecord = new APDUCommand(0x00, 0xB2, p1, p2, null, 0);
                    APDUResponse apduR = this.cardUpdater.Transmit(apduReadRecord);
                    if (apduR.SW1 == 0x90)
                    {
                        if (apduR.Data[0] == 0x70 || apduR.Data[0] == 0x77)
                        {
                            ExtractData(ReadTagData(apduR.Data, 0));
                            //if (!String.IsNullOrEmpty(NumberString) && !String.IsNullOrEmpty(Name) &&
                            //    !String.IsNullOrEmpty(ExpiryString) && !String.IsNullOrEmpty(CardType) &&
                            //    !String.IsNullOrEmpty(Label))
                            //    return;  //we have all info we need
                        }
                        else
                            throw new Exception("Unrecognized data template");
                    }
                }
            }

        }

        private void ExtractData(byte[] data)
        {
            int i = 0;
            byte[] tagdata;
            while (i < data.Length)
            {
                int tag = ReadTag(data, i);
                tagdata = ReadTagData(data, i);
                i = SkipTag(data, i);

                if (IsTemplate(tag))  //its a template - has more data inside
                {
                    ExtractData(tagdata);
                    continue;
                }

                Properties.Add(new TagData(tag, tagdata));

                switch (tag)
                {
                    case 0x57:  //track 2 equivalent data
                        //card number
                        Number = ConvertBCDCardNumber(tagdata, 0, 8);
                        //get the string form XXXX XXXX XXXX XXXX
                        StringBuilder sb = new StringBuilder();
                        sb.Append(ConvertBCDCardNumber(tagdata, 0, 2).ToString("D4"));
                        sb.Append(" ");
                        sb.Append(ConvertBCDCardNumber(tagdata, 2, 2).ToString("D4"));
                        sb.Append(" ");
                        sb.Append(ConvertBCDCardNumber(tagdata, 4, 2).ToString("D4"));
                        sb.Append(" ");
                        sb.Append(ConvertBCDCardNumber(tagdata, 6, 2).ToString("D4"));
                        NumberString = sb.ToString();

                        //expiry date, converting from bcd
                        byte month = (byte)(10 * (tagdata[9] & 0xf) + ((tagdata[10] >> 4) & 0xf));
                        byte year = (byte)(10 * (tagdata[8] & 0xf) + ((tagdata[9] >> 4) & 0xf));
                        Expiry = new DateTime(2000 + year, month, 1);
                        ExpiryString = Expiry.ToString("MMM yyyy");
                        break;

                    case 0x5F20:    //cardholder name
                        Name = ASCIIEncoding.ASCII.GetString(tagdata).Trim();
                        break;

                    case 0x50:  //application label (mastercard or visacredit)
                        CardType = ASCIIEncoding.ASCII.GetString(tagdata).Trim();
                        if (String.IsNullOrEmpty(Label))    //so that we don't override application preferred label which has priority
                            Label = CardType;
                        break;
                    case 0x9f12:  //application preferred label
                        Label = ASCIIEncoding.ASCII.GetString(tagdata).Trim();
                        break;
                }
            }

        }

        //checks whether a tag is a template
        private bool IsTemplate(int tag)
        {
            switch (tag)
            {
                case 0xa5:  //its a template - has more data inside
                case 0x61:
                case 0x6f:
                case 0x70:
                case 0x71:
                case 0x72:
                case 0x73:
                case 0x77:
                case 0x80:
                case 0xbf0c:
                    return true;
            }
            return false;
        }

        private void ReadGPO()
        {
            //get processing options
            APDUCommand apduGPO = new APDUCommand(0x80, 0xa8, 0, 0, new byte[] { 0x83, 0 }, 0);
            APDUResponse apdu1 = this.cardUpdater.Transmit(apduGPO);
            if (apdu1.SW1 != 0x90) throw new Exception("Read GPO Data fail");
            //two possible forms, 0x80 and 0x77
            if (apdu1.Data[0] == 0x80)
            {
                for (int i = 4; i < apdu1.Data.Length; i += 4)
                {
                    byte sfi = (byte)((apdu1.Data[i] >> 3) & 0xf);
                    byte lowRange = apdu1.Data[i + 1];
                    byte hiRange = apdu1.Data[i + 2];
                    byte[] records = new byte[hiRange - lowRange + 1];
                    for (int j = lowRange; j <= hiRange; j++)
                        records[j - lowRange] = (byte)j;
                    sfiRecords.Add(new SFIRecords(sfi, records));
                }
            }
            else if (apdu1.Data[0] == 0x77)
            {
                //look for the application file locator AFL
                int a, tag;
                for (a = 2; (tag = ReadTag(apdu1.Data, a)) != 0x94; a = SkipTag(apdu1.Data, a)) ;
                if (tag == 0x94)
                {
                    //found it
                    a++;
                    int len = apdu1.Data[a++];
                    for (int i = a; i < a + len; i += 4)
                    {
                        byte sfi = (byte)((apdu1.Data[i] >> 3) & 0xf);
                        byte lowRange = apdu1.Data[i + 1];
                        byte hiRange = apdu1.Data[i + 2];
                        byte[] records = new byte[hiRange - lowRange + 1];
                        for (int j = lowRange; j <= hiRange; j++)
                            records[j - lowRange] = (byte)j;
                        sfiRecords.Add(new SFIRecords(sfi, records));
                    }
                }
            }
            else
                throw new Exception("Unknown GPO template");
        }

        //returns the ber-tlv tag
        private int ReadTag(byte[] data, int index)
        {
            int i = index;
            byte tag = data[i++];
            int fulltag = tag;
            if ((tag & 0x1f) == 0x1f)
            {
                //tag has two or more bytes
                do
                {
                    tag = data[i++];
                    fulltag = (fulltag << 8) | tag;
                } while ((tag & 0x80) == 0x80);
            }
            return fulltag;
        }

        //returns new index after skipping over current ber-tlv tag.
        private int SkipTag(byte[] data, int index)
        {
            int i = index;
            byte tag = data[i++];
            int len = 0;
            if ((tag & 0x1f) == 0x1f)
            {
                //tag has two or more bytes
                do
                {
                    tag = data[i++];
                } while ((tag & 0x80) == 0x80);
            }

            len = data[i++];
            if ((len & 0x80) == 0x80)
            {
                //len has more than one byte
                int lenlen = len & 0x7f;    //length of length
                len = 0;
                for (int l = 0; l < lenlen; l++)
                {
                    len = (len << 8) | data[i++];
                }
            }

            return i + len;
        }

        //read the current tag's data
        private byte[] ReadTagData(byte[] data, int index)
        {
            int i = index;
            byte tag = data[i++];
            int len = 0;
            if ((tag & 0x1f) == 0x1f)
            {
                //tag has two or more bytes
                do
                {
                    tag = data[i++];
                } while ((tag & 0x80) == 0x80);
            }

            len = data[i++];
            if ((len & 0x80) == 0x80)
            {
                //len has more than one byte
                int lenlen = len & 0x7f;    //length of length
                len = 0;
                for (int l = 0; l < lenlen; l++)
                {
                    len = (len << 8) | data[i++];
                }
            }

            //we should now be at the data
            byte[] tagdata = new byte[len];
            Array.Copy(data, i, tagdata, 0, len);
            return tagdata;
        }

        //convert bcd coded card number
        private static long ConvertBCDCardNumber(byte[] dataBytes, int offset, int len)
        {
            long cardnum = 0;
            for (int b = offset; b < offset + len; b++)
                cardnum = (cardnum * 100) + ((dataBytes[b] >> 4) & 0xf) * 10 + (dataBytes[b] & 0xf);
            return cardnum;
        }

    }

    public class TagData
    {
        //tag number -> tag name
        private static readonly Dictionary<int, string> tagNames = new Dictionary<int, string>
        {
            {0x5f57, "Account Type"},
            {0x9f01, "Acquirer Identifier"},
            {0x9f40, "Additional Terminal Capabilities"},
            {0x81, "Amount, Authorized (Binary)"},
            {0x9f02, "Amount, Authorized (Numeric)"},
            {0x9f04, "Amount, Other (Binary)"},
            {0x9f03, "Amount, Other (Numeric)"},
            {0x9f3a, "Amount, Reference Currency"},
            {0x9f26, "Application Cyptogram"},
            {0x9f42, "Application Currency Code"},
            {0x9f44, "Application Currency Exponent"},
            {0x9f05, "Application Discretionary Data"},
            {0x5f25, "Application Effective Date"},
            {0x5f24, "Application Expiration Date"},
            {0x94, "Application File Locator (AFL)"},
            {0x4f, "Application Identifier (AID) - card"},
            {0x9f06, "Application Identifier (AID) - terminal"},
            {0x82, "Application Interchange Profile"},
            {0x50, "Application Label"},
            {0x9f12, "Application Preferred Name"},
            {0x5a, "Application Primary Account Number (PAN)"},
            {0x5f34, "Application Primary Account Number (PAN) Sequence Number"},
            {0x87, "Application Priority Indicator"},
            {0x9f3b, "Application Reference Currency"},
            {0x9f43, "Application Reference Currency Exponent"},
            {0x61, "Application Template"},
            {0x9f36, "Application Transaction Counter (ATC)"},
            {0x9f07, "Application Usage Control"},
            {0x9f08, "Application Version Number"},
            {0x9f09, "Application Version Number"},
            {0x89, "Authorization Code"},
            {0x8a, "Authorization Response Code"},
            {0x5f54, "Bank Identifier Code (BIC)"},
            {0x8c, "Card Risk Management Data Object List 1 (CDOL1)"},
            {0x8d, "Card Risk Management Data Object List 2 (CDOL2)"},
            {0x5f20, "Cardholder Name"},
            {0x9f0b, "Cardholder Name Extended"},
            {0x8e, "Card Verification Method (CVM) List"},
            {0x9f34, "Card Verification Method (CVM) Results"},
            {0x8f, "Certification Authority Public Key Index"},
            {0x9f22, "Certification Authority Public Key Index"},
            {0x83, "Command Template"},
            {0x9f27, "Cryptogram Information Data"},
            {0x9f45, "Data Authentication Code"},
            {0x84, "Dedicated File (DF) Name"},
            {0x9d, "Directory Definition File (DDF) Name"},
            {0x73, "Directory Discretionary Template"},
            {0x9f49, "Dynamic Data Authentication Data Object List (DDOL)"},
            {0xbf0c, "File Control Information (FCI) Issuer Discretionary Data"},
            {0xa5, "File Control Information (FCI) Proprietary Template"},
            {0x6f, "File Control Information (FCI) Template"},
            {0x9f4c, "ICC Dynamic Number"},
            {0x9f2d, "Integrated Circuit Card (ICC) PIN Encipherment Public Key Certificate"},
            {0x9f2e, "Integrated Circuit Card (ICC) PIN Encipherment Public Key Exponent"},
            {0x9f2f, "Integrated Circuit Card (ICC) PIN Encipherment Public Key Remainder"},
            {0x9f46, "Integrated Circuit Card (ICC) Public Key Certificate"},
            {0x9f47, "Integrated Circuit Card (ICC) Public Key Exponent"},
            {0x9f48, "Integrated Circuit Card (ICC) Public Key Remainder"},
            {0x9f1e, "Interface Device (IFD) Serial Number"},
            {0x5f53, "International Bank Account Number (IBAN)"},
            {0x9f0d, "Issuer Action Code - Default"},
            {0x9f0e, "Issuer Action Code - Denial"},
            {0x9f0f, "Issuer Action Code - Online"},
            {0x9f10, "Issuer Application Data"},
            {0x91, "Issuer Authentication Data"},
            {0x9f11, "Issuer Code Table Index"},
            {0x5f28, "Issuer Country Code"},
            {0x5f55, "Issuer Country Code (alpha2 format)"},
            {0x5f56, "Issuer Country Code (alpha3 format)"},
            {0x42, "Issuer Identification Number (IIN)"},
            {0x90, "Issuer Public Key Certificate"},
            {0x9f32, "Issuer Public Key Exponent"},
            {0x92, "Issuer Public Key Remainder"},
            {0x86, "Issuer Script Command"},
            {0x9f18, "Issuer Script Identifier"},
            {0x71, "Issuer Script Template 1"},
            {0x72, "Issuer Script Template 2"},
            {0x5f50, "Issuer URL"},
            {0x5f2d, "Language Preference"},
            {0x9f13, "Last Online Application Transaction Counter (ATC) Register"},
            {0x9f4d, "Log Entry"},
            {0x9f4f, "Log Format"},
            {0x9f14, "Lower Consecutive Offline Limit"},
            {0x9f15, "Merchant Category Code"},
            {0x9f16, "Merchant Identifier"},
            {0x9f4e, "Merchant Name and Location"},
            {0x9f17, "Personal Identification Number (PIN) Try Counter"},
            {0x9f39, "Point-of-Service (POS) Entry Mode"},
            {0x9f38, "Processing Data Options Object List (PDOL)"},
            {0x70, "READ RECORD Response Message Template"},
            {0x80, "Response Message Template Format 1"},
            {0x77, "Response Message Template Format 2"},
            {0x5f30, "Service Code"},
            {0x88, "Short File Identifier (SFI)"},
            {0x9f4b, "Signed Dynamic Application Data"},
            {0x93, "Signed Static Application Data"},
            {0x9f4a, "Static Data Authentication Tag List"},
            {0x9f33, "Terminal Capabilities"},
            {0x9f1a, "Terminal Country Code"},
            {0x9f1b, "Terminal Floor Limit"},
            {0x9f1c, "Terminal Identification"},
            {0x9f1d, "Terminal Risk Management Data"},
            {0x9f35, "Terminal Type"},
            {0x95, "Terminal Verification Results"},
            {0x9f1f, "Track 1 Discretionary Data"},
            {0x9f20, "Track 2 Discretionary Data"},
            {0x57, "Track 2 Equivalent Data"},
            {0x97, "Transaction Certificate Data Object List (TDOL)"},
            {0x98, "Transaction Certificate (TC) Hash Value"},
            {0x5f2a, "Transaction Currency Code"},
            {0x5f36, "Transaction Currency Exponent"},
            {0x9a, "Transaction Date"},
            {0x99, "Transaction Personal Identification Number (PIN) Data"},
            {0x9f3c, "Transaction Reference Currency Code"},
            {0x9f3d, "Transaction Reference Currency Exponent"},
            {0x9f41, "Transaction Sequence Counter"},
            {0x9b, "Transaction Status Information"},
            {0x9f21, "Transaction Time"},
            {0x9c, "Transaction Type"},
            {0x9f37, "Unpredictable Number"},
            {0x9f23, "Upper Consecutive Offline Limit"}
        };


        public string Name { get; set; }
        public int Id { get; set; }
        public byte[] Data { get; set; }
        public string DataString { get; set; }

        public TagData(int tagId, byte[] tagdata)
        {
            this.Id = tagId;
            this.Data = tagdata;
            this.Name = tagNames[tagId];
            //check if data is a string
            bool isString = true;
            foreach (byte b in Data)
            {
                if (b < 0x20 || b > 0x7f)
                {
                    isString = false;
                    break;
                }
            }
            this.DataString = isString ? ASCIIEncoding.ASCII.GetString(tagdata).Trim() : ToHexString(tagdata, 0, tagdata.Length);
        }

        private static string ToHexString(byte[] bstr, int idx, int len)
        {
            string str = "";
            for (int i = 0; i < len; )
            {
                str += bstr[idx + i].ToString("X2") + " ";
                i++;
                if (i % 16 == 0) str += "\r\n";
            }
            return str;
        }
    }
}
