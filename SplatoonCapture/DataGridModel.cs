using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplatoonCapture
{
    public class DataGridModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private KillItem[] _capKillItems;
        public KillItem[] CapKillItems
        {
            get { return _capKillItems; }
            set
            {
                _capKillItems = value;
                var h = PropertyChanged;
                if (h != null) h(this, new PropertyChangedEventArgs("CapKillItems"));
            }
        }
        private DeathItem[] _capDeathItems;
        public DeathItem[] CapDeathItems
        {
            get { return _capDeathItems; }
            set
            {
                _capDeathItems = value;
                var h = PropertyChanged;
                if (h != null) h(this, new PropertyChangedEventArgs("CapDeathItems"));
            }
        }
        private SalmonDeathItem[] _capSalmonDeathItems;
        public SalmonDeathItem[] CapSalmonDeathItems
        {
            get { return _capSalmonDeathItems; }
            set
            {
                _capSalmonDeathItems = value;
                var h = PropertyChanged;
                if (h != null) h(this, new PropertyChangedEventArgs("CapSalmonDeathItems"));
            }
        }

        public DataGridModel()
        {
            this.CapKillItems = new KillItem[0];
            this.CapDeathItems = new DeathItem[0];
            this.CapSalmonDeathItems = new SalmonDeathItem[0];
        }
    }


    public class KillItem
    {
        public int No { get; set; } = 0;
        public string Name { get; set; } = string.Empty;

        public KillItem(int no, string name)
        {
            No = no;
            Name = name;
        }
    }
    public class DeathItem
    {
        public int No { get; set; } = 0;
        public string Name { get; set; } = string.Empty;

        public DeathItem(int no, string name)
        {
            No = no;
            Name = name;
        }
    }
    public class SalmonDeathItem
    {
        public int No { get; set; } = 0;
        public string Name { get; set; } = string.Empty;

        public SalmonDeathItem(int no, string name)
        {
            No = no;
            Name = name;
        }
    }
}
