using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CardReaderGui
{
    public class UnknownCard : ISmartCard
    {
        #region ISmartCard Members

        public IList<object> Properties { get; set; }

        public bool SelectApplication()
        {
            return true;
        }

        public void ReadCard()
        {
        }

        #endregion
    }
}
