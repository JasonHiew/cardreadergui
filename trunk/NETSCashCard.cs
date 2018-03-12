using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CardReaderGui
{
    public class NETSCashCard : ISmartCard
    {
        private CardNative iCard;

        public double Balance { get; set; }
        public long Number { get; set; }
        public string NumberString { get; set; }
        public DateTime Expiry { get; set; }
        public string ExpiryString { get; set; }
        public double Deposit { get; set; }
        public IList<NETSTransaction> TransactionLog { get; set; }
        public IList<object> Properties { get; set; }

        public NETSCashCard(CardNative reader)
        {
            this.iCard = reader;
            this.TransactionLog = new List<NETSTransaction>();
        }

        #region ISmartCard Members

        public bool SelectApplication()
        {
            System.Threading.Thread.Sleep(100);
            //dunno what the heck is this command
            APDUCommand apduCmd = new APDUCommand(0, 0xa4, 1, 0, new byte[]{1,0x18}, 0);
            APDUResponse apdu1 = iCard.Transmit(apduCmd);
            if (apdu1.SW1 != 0x90)
            {
                System.Threading.Thread.Sleep(100);
                apdu1 = iCard.Transmit(apduCmd);
                if (apdu1.SW1 != 0x90)
                    return false;
            }

            System.Threading.Thread.Sleep(100);
            //dunno what the heck is this command, probably selecting something
            apduCmd = new APDUCommand(0, 0xa4, 2, 0, new byte[] { 0, 1 }, 0);
            apdu1 = iCard.Transmit(apduCmd);
            if (apdu1.SW1 != 0x90)
            {
                System.Threading.Thread.Sleep(100);
                apdu1 = iCard.Transmit(apduCmd);
                if (apdu1.SW1 != 0x90)
                    return false;
            }
            System.Threading.Thread.Sleep(100);

            return true;
        }

        public void ReadCard()
        {
            //read balance   80 32 00 03 04 00 00 00 00
            //received:                                     Deposit(BCD)
            //                                              vvvvv
            // 05 11 11 25 02 03 12 15 37 20 00 25 09 08 60 00 00 0A 00 18 00 00 00 00 90 00
            //    ^^^^^^^^^^^^^^^^^^^^^^^          ^^^^^ ^^
            //            CAN                issue date  months of validity
            //                               2009/08     60 mths

            APDUCommand apduCmd = new APDUCommand(0x80, 0x32, 0, 3, new byte[4], 0);
            APDUResponse apdu1 = iCard.Transmit(apduCmd);
            byte[] balanceBytes = new byte[4];
            balanceBytes[0] = apdu1.Data[3];
            balanceBytes[1] = apdu1.Data[2];
            balanceBytes[2] = apdu1.Data[1];
            balanceBytes[3] = apdu1.Data[0];
            //balance is in cents. divide by 100 to get dollars
            Balance = BitConverter.ToInt32(balanceBytes, 0) / 100.0f;

            //dunno what the heck is this command, probably selecting something
            apduCmd = new APDUCommand(0, 0xa4, 2, 0, new byte[] { 0, 1 }, 0);
            apdu1 = iCard.Transmit(apduCmd);

            //read CAN, expiry date, deposit 00 B0 00 00 18
            apduCmd = new APDUCommand(0x00, 0xb0, 0, 0, null, 0x18);
            apdu1 = iCard.Transmit(apduCmd);
            //card number (CAN)
            Number = ConvertBCDCardNumber(apdu1.Data, 1, 8);
            //get the string form XXXX-XXXX-XXXX-XXXX
            StringBuilder sb = new StringBuilder();
            sb.Append(ConvertBCDCardNumber(apdu1.Data, 1, 2).ToString("D4"));
            sb.Append("-");
            sb.Append(ConvertBCDCardNumber(apdu1.Data, 3, 2).ToString("D4"));
            sb.Append("-");
            sb.Append(ConvertBCDCardNumber(apdu1.Data, 5, 2).ToString("D4"));
            sb.Append("-");
            sb.Append(ConvertBCDCardNumber(apdu1.Data, 7, 2).ToString("D4"));
            NumberString = sb.ToString();
            //read expiry date
            int year = apdu1.Data[12]; //year, from 1900 or 2000
            if (year > 90 && year < 100)
                year += 1900;
            else year += 2000;
            byte month = apdu1.Data[13];
            byte validity = (byte)ConvertBCDCardNumber(apdu1.Data, 14, 1); //validity in months
            DateTime issueDate = new DateTime(year, month, 1);
            Expiry = issueDate.AddMonths(validity);
            ExpiryString = Expiry.ToString("dd MMM yyyy");
            Deposit = ConvertBCDCardNumber(apdu1.Data, 15, 2) / 100.0f;

            //get transaction log
            //read number of total entries in log
            apduCmd = new APDUCommand(0, 0xb0, 0, 2, null, 4);
            apdu1 = iCard.Transmit(apduCmd);
            if (apdu1.SW1 != 0x90) throw new Exception("Read NETS log size fail");
            short logSize = (short)ConvertBCDCardNumber(new byte[]{apdu1.Data[2], apdu1.Data[3]}, 0, 2);

            apduCmd = new APDUCommand(0, 0xa4, 2, 0, new byte[] { 0, 5 }, 0);
            apdu1 = iCard.Transmit(apduCmd);
            if (apdu1.SW1 != 0x90) throw new Exception("Read NETS transaction fail");

            //read the current log entry number
            short endOffs = (short)(logSize * 4);
            byte endP1 = (byte)((endOffs >> 8) & 0xff);
            byte endP2 = (byte)(endOffs & 0xff);
            apduCmd = new APDUCommand(0, 0xb0, endP1, endP2, null, 2);
            apdu1 = iCard.Transmit(apduCmd);
            if (apdu1.SW1 != 0x90) throw new Exception("Read NETS current log number fail");
            short currentLogNum = (short)ConvertBCDCardNumber(new byte[] { apdu1.Data[0], apdu1.Data[1] }, 0, 2);
            currentLogNum -= 2;

            //read each log entry from the ring buffer, starting from current
            for (byte n = 0; n < logSize; n++)
            {
                int i = currentLogNum - n;
                if (i < 0) i += logSize;
                short logOffs = (short)(i * 4);
                byte logP1 = (byte)((logOffs >> 8) & 0xff);
                byte logP2 = (byte)(logOffs & 0xff);
                //read binary 0xb0, offset logOffs, size 16
                apduCmd = new APDUCommand(0, 0xb0, logP1, logP2, null, 16);
                apdu1 = iCard.Transmit(apduCmd);
                if (apdu1.SW1 != 0x90) throw new Exception("Read NETS log entry fail");
                NETSTransaction logEntry = new NETSTransaction(apdu1.Data);
                if (logEntry.Type != 0)
                    TransactionLog.Add(logEntry);
            }
        }

        #endregion

        //convert bcd coded card number
        private static long ConvertBCDCardNumber(byte[] dataBytes, int offset, int len)
        {
            long cardnum = 0;
            for (int b = offset; b < offset + len; b++)
                cardnum = (cardnum * 100) + ((dataBytes[b] >> 4) & 0xf) * 10 + (dataBytes[b] & 0xf);
            return cardnum;
        }

    }

    /*
# Types
01  = Purchase
02  = Cash Top Up
03  = NETS Top Up
04  = Statement Print
07  = ERP
09  = EPS Car Park
10  = Cash Refund
11  = BlackList
18  = Bucket System
20  = NETS Refund
33  = Internet Payment
36  = Car Park
41  = Top Up
42  = Top Up
64  = Quick Top Up
65  = Quick Savings
74  = HOMENETS Top Up
71  = CashCard Online Top-up
77  = Share ATM Refund
78  = ATM Refund
79  = Shared ATM Top Up
80  = ATM Top Up
81  = ATM Top Up
82  = ATM Top Up
96  = Purchase Error 96
97  = Purchase Error 97
98  = Purchase Error 98
99  = Purchase Error 99
     */
    public class NETSTransaction
    {
        public byte Type { get; set; }
        public string TypeString { get; set; }
        public double Amount { get; set; }
        public DateTime Date { get; set; }
        public string DateString { get; set; }
        public string Merchant { get; set; }

        /*
 received:
  59 00 05 00 1B D7 CC B8 56 45 50 20 20 20 00 00 90 00
  ^^ ^^^^^^^^ ^^^^^^^^^^^ ^^^^^^^^^^^^^^^^^
type amount   date        merchant
BCD  BCD
         */
        public NETSTransaction(byte[] transactionBytes)
        {
            Type = (byte)ConvertBCDCardNumber(transactionBytes, 0, 1);
            TypeString = TypeToString(Type);
            long amount = ConvertBCDCardNumber(transactionBytes, 1, 3);
            Amount = amount / 100.0f;
            byte[] dateBytes = new byte[] { transactionBytes[7], transactionBytes[6], transactionBytes[5], transactionBytes[4] };
            int dateInt = BitConverter.ToInt32(dateBytes, 0);   //number of seconds since jan 1, 1995
            DateTime jan1_95 = new DateTime(1995, 1, 1);
            Date = jan1_95.AddSeconds(dateInt);
            DateString = Date.ToString("dd MMM yyyy h':'mm':'ss tt");
            if (Type == 7 || Type == 9)  //these don't have merchant name, but a number
                Merchant = ConvertBCDCardNumber(transactionBytes, 13, 1).ToString();
            else
                Merchant = ASCIIEncoding.ASCII.GetString(transactionBytes, 8, 6).Trim();
        }

        //convert bcd coded card number
        private static long ConvertBCDCardNumber(byte[] dataBytes, int offset, int len)
        {
            long cardnum = 0;
            for (int b = offset; b < offset + len; b++)
                cardnum = (cardnum * 100) + ((dataBytes[b] >> 4) & 0xf) * 10 + (dataBytes[b] & 0xf);
            return cardnum;
        }

        private static string TypeToString(byte type)
        {
            switch (type)
            {
                case 1: return "Purchase";
                case 2: return "Cash Top Up";
                case 3: return "NETS Top Up";
                case 4: return "Statement Print";
                case 7: return "ERP";
                case 9: return "EPS Car Park";
                case 10: return "Cash Refund";
                case 11: return "BlackList";
                case 18: return "Bucket System";
                case 20: return "NETS Refund";
                case 33: return "Internet Payment";
                case 36: return "Car Park";
                case 41: return "Top Up";
                case 42: return "Top Up";
                case 64: return "Quick Top Up";
                case 65: return "Quick Savings";
                case 74: return "HOMENETS Top Up";
                case 71: return "CashCard Online Top-up";
                case 77: return "Share ATM Refund";
                case 78: return "ATM Refund";
                case 79: return "Shared ATM Top Up";
                case 80: return "ATM Top Up";
                case 81: return "ATM Top Up";
                case 82: return "ATM Top Up";
                case 96: return "Purchase Error 96";
                case 97: return "Purchase Error 97";
                case 98: return "Purchase Error 98";
                case 99: return "Purchase Error 99";
                default: return "Type " + type.ToString();
            }
        }
    }
}
