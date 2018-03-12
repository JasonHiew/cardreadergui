using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Data;

namespace CardReaderGui
{
    public class MyKad : ISmartCard
    {
        private CardNative cardUpdater;
        private string errorMsg;

        private byte[] m_JPNAID = new byte[] { 0xA0, 0x00, 0x00, 0x00, 0x74, 0x4A, 0x50, 0x4E, 0x00, 0x10 };

        private byte[] jpn1_1 = new byte[459];  //this is name n basic info
        private byte[] jpn1_2 = new byte[4011]; //this is the photo
        private byte[] jpn1_4 = new byte[171];  //this is address

        public string Name { get; set; }
        public string IC { get; set; }
        public string Sex { get; set; }
        public string OldIC { get; set; }
        public DateTime BirthDate { get; set; }
        public string BirthDateString { get; set; }
        public string BirthPlace { get; set; }
        public DateTime IssueDate { get; set; }
        public string IssueDateString { get; set; }
        public string Citizenship { get; set; }
        public string Race { get; set; }
        public string Religion { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public MemoryStream Photo { get; set; } //as JPG

        /// <summary>
        /// The complete address string
        /// </summary>
        public string Address
        {
            get
            {
                StringBuilder address = new StringBuilder();
                address.Append(Address1);
                if (!String.IsNullOrEmpty(Address2))
                    address.Append("\n" + Address2);
                if (!String.IsNullOrEmpty(Address3))
                    address.Append("\n" + Address3);
                return address.ToString();
            }
        }
        public int Postcode { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public IList<object> Properties { get; set; }

        public MyKad(CardNative reader)
        {
            this.cardUpdater = reader;
        }

        public bool SelectApplication()
        {
            APDUCommand apduSelectPxyApl = new APDUCommand(0x00, 0xA4, 4, 0, m_JPNAID, 0);
            APDUResponse apdu1 = this.cardUpdater.Transmit(apduSelectPxyApl);
            byte[] apduRespByte = apdu1.Packet;
            return checkExchangeApduResult(apduRespByte);
        }

        public void ReadCard()
        {
            //read jpn1_1 file
            SetLength((short)jpn1_1.Length);
            SelectInfo(1, 1, 0, (short)jpn1_1.Length);
            jpn1_1 = ReadInfo((short)jpn1_1.Length);
            Name = ASCIIEncoding.ASCII.GetString(jpn1_1, 3, 150).Trim();
            IC = ASCIIEncoding.ASCII.GetString(jpn1_1, 0x111, 13);
            Sex = ASCIIEncoding.ASCII.GetString(jpn1_1, 0x11e, 1) == "L" ? "Male" : "Female";
            OldIC = ASCIIEncoding.ASCII.GetString(jpn1_1, 0x11f, 8);
            BirthDate = ConvertBCDDate(jpn1_1, 0x127);
            BirthDateString = BirthDate.ToString("dd MMM yyyy");
            BirthPlace = ASCIIEncoding.ASCII.GetString(jpn1_1, 0x12b, 25).Trim();
            IssueDate = ConvertBCDDate(jpn1_1, 0x144);
            IssueDateString = IssueDate.ToString("dd MMM yyyy");
            Citizenship = ASCIIEncoding.ASCII.GetString(jpn1_1, 0x148, 18).Trim();
            Race = ASCIIEncoding.ASCII.GetString(jpn1_1, 0x15a, 25).Trim();
            Religion = ASCIIEncoding.ASCII.GetString(jpn1_1, 0x173, 11).Trim();

            //read jpn1_2 file
            SetLength((short)jpn1_2.Length);
            SelectInfo(1, 2, 0, (short)jpn1_2.Length);
            jpn1_2 = ReadInfo((short)jpn1_2.Length);
            Photo = new MemoryStream(jpn1_2, 3, 4000);
            Photo.Seek(0, SeekOrigin.Begin);

            //read jpn1_4 file
            SetLength((short)jpn1_4.Length);
            SelectInfo(1, 4, 0, (short)jpn1_4.Length);
            jpn1_4 = ReadInfo((short)jpn1_4.Length);
            Address1 = ASCIIEncoding.ASCII.GetString(jpn1_4, 3, 30).Trim();
            Address2 = ASCIIEncoding.ASCII.GetString(jpn1_4, 0x21, 30).Trim();
            Address3 = ASCIIEncoding.ASCII.GetString(jpn1_4, 0x3f, 30).Trim();
            Postcode = ConvertBCDPostcode(jpn1_4, 0x5d);
            City = ASCIIEncoding.ASCII.GetString(jpn1_4, 0x60, 25).Trim();
            State = ASCIIEncoding.ASCII.GetString(jpn1_4, 0x79, 30).Trim();
        }

        //convert bcd coded year,month,day YYYYMMDD
        private static DateTime ConvertBCDDate(byte[] dataBytes, int offset)
        {
            int year = 0;
            for (int b = offset; b < offset + 2; b++)
                year = (year * 100) + ((dataBytes[b] >> 4) & 0xf) * 10 + (dataBytes[b] & 0xf);
            int month = ((dataBytes[offset + 2] >> 4) & 0xf) * 10 + (dataBytes[offset + 2] & 0xf);
            int day = ((dataBytes[offset+3] >> 4) & 0xf) * 10 + (dataBytes[offset+3] & 0xf);
            return new DateTime(year, month, day);
        }

        //convert bcd coded postcode
        private static int ConvertBCDPostcode(byte[] dataBytes, int offset)
        {
            int postcode = 0;
            for (int b = offset; b < offset + 3; b++)
                postcode = (postcode * 100) + ((dataBytes[b] >> 4) & 0xf) * 10 + (dataBytes[b] & 0xf);
            return postcode / 10;
        }

        private void SetLength(short len)
        {
            byte[] paramBytes = new byte[] { 8, 0, 0, 0, 0 };
            byte[] lenBytes = BitConverter.GetBytes(len);
            Array.Copy(lenBytes, 0, paramBytes, 3, 2); 
            APDUCommand setLengthAPdu = new APDUCommand(0xc8, 0x32, 0, 0, paramBytes, 0);
            APDUResponse apdu1 = this.cardUpdater.Transmit(setLengthAPdu);
            if (apdu1.SW1 != 0x91 || apdu1.SW2 != 8) throw new Exception("Failed to read"); ;
        }

        private void SelectInfo(short filen1, short filen2, short offset, short len)
        {
            byte[] paramBytes = new byte[8];
            byte[] n1Bytes = BitConverter.GetBytes(filen1);
            byte[] n2Bytes = BitConverter.GetBytes(filen2);
            byte[] offsetBytes = BitConverter.GetBytes(offset);
            byte[] lenBytes = BitConverter.GetBytes(len);
            Array.Copy(n2Bytes, 0, paramBytes, 0, 2);
            Array.Copy(n1Bytes, 0, paramBytes, 2, 2);
            Array.Copy(offsetBytes, 0, paramBytes, 4, 2);
            Array.Copy(lenBytes, 0, paramBytes, 6, 2);
            APDUCommand setLengthAPdu = new APDUCommand(0xcc, 0, 0, 0, paramBytes, 0);
            APDUResponse apdu1 = this.cardUpdater.Transmit(setLengthAPdu);
            if (apdu1.SW1 != 0x94) throw new Exception("Failed to read"); ;
        }

        private byte[] ReadInfo(short len)
        {
            MemoryStream ms = new MemoryStream();
            while (len > 0)
            {
                byte readLen = (byte)(len < 255 ? len : 255);
                APDUCommand readInfoApdu = new APDUCommand(0xcc, 6, 0, 0, null, readLen);
                APDUResponse apdu1 = this.cardUpdater.Transmit(readInfoApdu);
                if (apdu1.SW1 != 0x94 && apdu1.SW1 != 0x90) throw new Exception("Failed to read"); ;
                len -= readLen;
                ms.Write(apdu1.Data, 0, apdu1.Data.Length);
            }
            return ms.ToArray();
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

    }

    public class MemoryStreamToImageConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            MemoryStream ms = value as MemoryStream;
            if (ms == null) return null;
            BitmapImage photo = new BitmapImage();
            photo.BeginInit();
            photo.StreamSource = ms;
            photo.EndInit();
            return photo;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

}
