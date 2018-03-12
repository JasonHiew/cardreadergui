using System;
using System.Collections.Generic;
using System.Text;

namespace CardReaderGui
{
    public class TouchNGoCard : ISmartCard
    {
        private CardNative cardUpdater;
        private byte[] cardBalance = new byte[16];
        private byte[] cardExpireDate = new byte[2];
        private byte[] bCardSerialNo = new byte[9];
        private byte[] cardMfgSerialNo = new byte[4];
        private byte issuerFlag;
        private byte[] identificationCode = new byte[2];
        private byte companyCode;
        private byte code1;
        private byte[] accountCode = new byte[4];
        private byte luhnKey;
        private byte[] lastReloadDate = new byte[4];
        private byte[] lastReloadAmount = new byte[2];
        private byte[] lastAftReloadBal = new byte[4];
        private byte blackListFlag;
        public String debugString = null;
        private String errorMsg = "";
        private bool successReadManufacturer;
        private bool successReadReloadHistory;
        private bool successReadCardInfo;
        private bool successReadBalance;
        private bool successReadBlackList;
        private byte[] m_proxyAppletAID = new byte[] { 0xA0, 00, 00, 02, 09, 0x4D, 0x69, 0x66, 0x61, 0x72, 0x65, 01 };

        public IList<object> Properties { get; set; }

        /*public TouchNGoCard(byte[] paramArrayOfByte)
        {
          this.m_proxyAppletAID = paramArrayOfByte;
        }*/

        public TouchNGoCard(CardNative reader)
        {
            this.cardUpdater = reader;
        }

        public void ReadCard()
        {
            this.successReadManufacturer = true;
            this.successReadReloadHistory = true;
            this.successReadCardInfo = true;
            this.successReadBalance = true;
            this.successReadBlackList = true;
            byte b = 0xca;
            //int i = 0xb0;
            byte[] arrayOfByte = null;
            try
            {
                arrayOfByte = mifareGetData(b, 1, 0);
                this.cardMfgSerialNo[0] = arrayOfByte[0];
                this.cardMfgSerialNo[1] = arrayOfByte[1];
                this.cardMfgSerialNo[2] = arrayOfByte[2];
                this.cardMfgSerialNo[3] = arrayOfByte[3];
            }
            catch (Exception localException1)
            {
                this.successReadManufacturer = false;
            }
            try
            {
                arrayOfByte = mifareGetData(b, 2, 4);
                this.lastReloadAmount[0] = arrayOfByte[3];
                this.lastReloadAmount[1] = arrayOfByte[2];
                this.lastReloadDate[0] = arrayOfByte[7];
                this.lastReloadDate[1] = arrayOfByte[6];
                this.lastReloadDate[2] = arrayOfByte[5];
                this.lastReloadDate[3] = arrayOfByte[4];
                this.lastAftReloadBal[0] = arrayOfByte[8];
                this.lastAftReloadBal[1] = arrayOfByte[9];
                this.lastAftReloadBal[2] = arrayOfByte[10];
                this.lastAftReloadBal[3] = arrayOfByte[11];
            }
            catch (Exception localException2)
            {
                this.successReadReloadHistory = false;
            }
            try
            {
                arrayOfByte = mifareGetData(b, 2, 1);
                this.issuerFlag = arrayOfByte[1];
                this.cardExpireDate[0] = arrayOfByte[3];
                this.cardExpireDate[1] = arrayOfByte[2];
                this.identificationCode[0] = arrayOfByte[8];
                this.identificationCode[1] = arrayOfByte[7];
                this.companyCode = arrayOfByte[9];
                this.code1 = arrayOfByte[10];
                this.accountCode[0] = arrayOfByte[14];
                this.accountCode[1] = arrayOfByte[13];
                this.accountCode[2] = arrayOfByte[12];
                this.accountCode[3] = arrayOfByte[11];
                this.luhnKey = arrayOfByte[15];
                this.bCardSerialNo[0] = this.identificationCode[0];
                this.bCardSerialNo[1] = this.identificationCode[1];
                this.bCardSerialNo[2] = this.companyCode;
                this.bCardSerialNo[3] = this.code1;
                this.bCardSerialNo[4] = this.accountCode[0];
                this.bCardSerialNo[5] = this.accountCode[1];
                this.bCardSerialNo[6] = this.accountCode[2];
                this.bCardSerialNo[7] = this.accountCode[3];
                this.bCardSerialNo[8] = this.luhnKey;
            }
            catch (Exception localException3)
            {
                this.successReadCardInfo = false;
            }
            try
            {
                arrayOfByte = mifareGetData(b, 2, 2);
                this.cardBalance[0] = arrayOfByte[0];
                this.cardBalance[1] = arrayOfByte[1];
                this.cardBalance[2] = arrayOfByte[2];
                this.cardBalance[3] = arrayOfByte[3];
                this.cardBalance[4] = arrayOfByte[4];
                this.cardBalance[5] = arrayOfByte[5];
                this.cardBalance[6] = arrayOfByte[6];
                this.cardBalance[7] = arrayOfByte[7];
                this.cardBalance[8] = arrayOfByte[8];
                this.cardBalance[9] = arrayOfByte[9];
                this.cardBalance[10] = arrayOfByte[10];
                this.cardBalance[11] = arrayOfByte[11];
                this.cardBalance[12] = arrayOfByte[12];
                this.cardBalance[12] = arrayOfByte[13];
                this.cardBalance[14] = arrayOfByte[14];
                this.cardBalance[15] = arrayOfByte[15];
            }
            catch (Exception localException4)
            {
                this.successReadBalance = false;
            }
            try
            {
                arrayOfByte = mifareGetData(b, 2, 8);
                this.blackListFlag = arrayOfByte[10];
            }
            catch (Exception localException5)
            {
                this.successReadBlackList = false;
                //this.debugString = ("blacklist:" + HexUtil.convertByteArrayToHexString(arrayOfByte));
            }
        }

        public bool SelectApplication()
        {
            APDUCommand apduSelectPxyApl = new APDUCommand(0x00, 0xA4, 4, 0, m_proxyAppletAID, 0);
            APDUResponse apdu1 = this.cardUpdater.Transmit(apduSelectPxyApl);
            byte[] apduRespByte = apdu1.Packet;
            return checkExchangeApduResult(apduRespByte);
        }

        private byte[] mifareGetData(byte paramByte1, byte paramByte2, byte paramByte3)
        {
            /*int i = 0;
            byte[] arrayOfByte1 = new byte[5 + this.m_proxyAppletAID.Length];
            arrayOfByte1[0] = 0;
            arrayOfByte1[1] = 0xa4;
            arrayOfByte1[2] = 4;
            arrayOfByte1[3] = 0;
            arrayOfByte1[4] = (byte)this.m_proxyAppletAID.Length;
            Array.Copy(this.m_proxyAppletAID, 0, arrayOfByte1, 5, this.m_proxyAppletAID.Length);
            byte[] arrayOfByte2 = new byte[5];
            byte[] arrayOfByte3 = null;
            int j = 0;
            arrayOfByte2[0] = 0x80;
            arrayOfByte2[1] = paramByte1;
            arrayOfByte2[2] = paramByte2;
            arrayOfByte2[3] = paramByte3;
            arrayOfByte2[4] = 0;*/
            try
            {
                byte[] arrayOfByte3;
                //APDUCommand apduSelectPxyApl = new APDUCommand(0x00, 0xA4, 4, 0, m_proxyAppletAID, 0);
                //APDUResponse apdu1 = this.cardUpdater.Transmit(apduSelectPxyApl);
                //arrayOfByte3 = apdu1.Packet;
              //if (!checkExchangeApduResult(arrayOfByte3))
                //return arrayOfByte3;
                APDUResponse apdu3 = this.cardUpdater.Transmit(new APDUCommand(0x80, paramByte1, paramByte2, paramByte3, null, 1));
                arrayOfByte3 = apdu3.Packet;
                checkExchangeApduResult(arrayOfByte3);
                return arrayOfByte3;
                //j = arrayOfByte3.Length;
            }
            catch (Exception localException)
            {
                //localException.printStackTrace();
            }
            return null;
        }

        private bool checkExchangeApduResult(byte[] paramArrayOfByte)
        {
            if ((paramArrayOfByte[0] == 0x6a) && (paramArrayOfByte[1] == 0x81))
            {
                this.errorMsg = "Application Not Available";
                return false;
            }
            if ((paramArrayOfByte[0] == 0x6a) && (paramArrayOfByte[1] == 0x82))
            {
                this.errorMsg = "Application Not Available";
                return false;
            }
            if ((paramArrayOfByte[(paramArrayOfByte.Length - 2)] != 0x90) && (paramArrayOfByte[(paramArrayOfByte.Length - 1)] != 0))
            {
                this.errorMsg = "Application Not Available";
                return false;
            }
            return true;
        }

        public double CardBalance
        {
            get
            {
                int balance = BitConverter.ToInt32(this.cardBalance, 0);    //in sen
                return balance / 100.0f;
            }
        }

        /// <summary>
        /// MSB                                         LSB
        /// 15 14 13 12 11 10 09 08 07 06 05 04 03 02 01 00
        ///  -  Y  Y  Y  Y  Y  Y  M  M  M  M  D  D  D  D  D
        ///  Year is offset from 1990
        ///  Month and Day starts from 1
        /// </summary>
        /// <returns></returns>
        public DateTime CardExpireDate
        {
            get
            {
                short expireDate = BitConverter.ToInt16(this.cardExpireDate, 0);
                int day = expireDate & 0x1f;
                int month = (expireDate >> 5) & 0xf;
                int year = 1990 + ((expireDate >> 9) & 0x3f);
                return new DateTime(year, month, day);
            }
        }

        public string CardExpireDateString
        {
            get
            {
                DateTime date = CardExpireDate;
                return date.ToString("dd MMM yyyy");
            }
        }

        public byte[] getCardSerialNo()
        {
            return this.bCardSerialNo;
        }

        public int CardMfgSerialNo
        {
            get
            {
                return BitConverter.ToInt32(this.cardMfgSerialNo, 0);
            }
        }

        /// <summary>
        /// MSB                                                                                         LSB
        /// 31 30 29 28 27 26 25 24 23 22 21 20 19 18 17 16 15 14 13 12 11 10 09 08 07 06 05 04 03 02 01 00
        ///  h  h  h  h  h  m  m  m  m  m  m  s  s  s  s  s  s  Y  Y  Y  Y  Y  Y  M  M  M  M  D  D  D  D  D
        ///  Year is offset from 1990
        ///  Month and Day starts from 1
        /// </summary>
        /// <returns></returns>
        public DateTime LastReloadDate
        {
            get
            {
                int expireDateTime = BitConverter.ToInt32(this.lastReloadDate, 0);
                int day = expireDateTime & 0x1f;
                int month = (expireDateTime >> 5) & 0xf;
                int year = 1990 + ((expireDateTime >> 9) & 0x3f);
                int seconds = (expireDateTime >> 15) & 0x3f;
                int minutes = (expireDateTime >> 21) & 0x3f;
                int hours = (expireDateTime >> 27) & 0x1f;
                return new DateTime(year, month, day, hours, minutes, seconds);
            }
        }

        public string LastReloadDateString
        {
            get
            {
                DateTime date = LastReloadDate;
                return date.ToString("dd MMM yyyy h':'mm':'ss tt");
            }
        }

        public double LastReloadAmount
        {
            get
            {
                short amt = BitConverter.ToInt16(this.lastReloadAmount, 0);
                return amt / 100.0f;
            }
        }

        public double LastReloadFinalAmount
        {
            get
            {
                int balance = BitConverter.ToInt32(this.lastAftReloadBal, 0);    //in sen
                return balance / 100.0f;
            }
        }

        public short IdentificationCode
        {
            get
            {
                return BitConverter.ToInt16(this.identificationCode, 0);
            }
        }

        public byte CompanyCode
        {
            get
            {
                return this.companyCode;
            }
        }

        public byte Code1
        {
            get
            {
                return this.code1;
            }
        }

        public int AccountCode
        {
            get
            {
                return BitConverter.ToInt32(this.accountCode, 0);
            }
        }

        public byte LuhnKey
        {
            get
            {
                return this.luhnKey;
            }
        }

        public byte IssuerFlag
        {
            get
            {
                return this.issuerFlag;
            }
        }

        public bool BlackListFlag
        {
            get
            {
                return this.blackListFlag == 0 ? false : true;
            }
        }

        public bool isSuccessReadManufacturer()
        {
            return this.successReadManufacturer;
        }

        public bool isSuccessReadBalance()
        {
            return this.successReadBalance;
        }

        public bool isSuccessReadBlackList()
        {
            return this.successReadBlackList;
        }

        public bool isSuccessReadCardInfo()
        {
            return this.successReadCardInfo;
        }

        public bool isSuccessReadReloadHistory()
        {
            return this.successReadReloadHistory;
        }

        public String getErrorMsg()
        {
            return this.errorMsg;
        }

        public void setCardUpdater(CardNative paramCardUpdater)
        {
            this.cardUpdater = paramCardUpdater;
        }

    }
}
