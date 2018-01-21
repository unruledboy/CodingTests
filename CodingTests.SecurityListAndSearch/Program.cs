using System;
using System.Collections.Generic;
using System.Linq;

namespace CodingTests.SecurityListAndSearch
{
    class Program
    {
        static void Main(string[] args)
        {
            var securities = Enumerable.Range(1, 100).Select(a => new Security { InstrumentId = string.Concat("Foo", a % 2 == 0 ? "Holy" : "Crap") }).ToArray();
            var securityRepo = new SecurityRepository(securities);
            var holySecurities = securityRepo.GetSecurities("FooHoly");
            Console.WriteLine(holySecurities.Count() == 50);

            var someSecurity = holySecurities.First();
            someSecurity.InstrumentId = "FooCrap";
            holySecurities = securityRepo.GetSecurities("FooHoly");
            Console.WriteLine(holySecurities.Count() == 49);
            var crapecurities = securityRepo.GetSecurities("FooCrap");
            Console.WriteLine(crapecurities.Count() == 51);

            someSecurity.InstrumentId = "Foo Lala Land";
            holySecurities = securityRepo.GetSecurities("FooCrap");
            Console.WriteLine(holySecurities.Count() == 50);
            Console.Read();
        }
    }

    public class SecurityInstrumentIdChangedEventArgs
    {
        public string OldInstrumentId { get; set; }
        public Security Security { get; set; }
    }

    public class Security
    {
        public event EventHandler<SecurityInstrumentIdChangedEventArgs> InstrumentIdChanged;
        private string _instrumentId;

        public string InstrumentId
        {
            get { return _instrumentId; }
            set
            {
                if (value != _instrumentId)
                {
                    var args = new SecurityInstrumentIdChangedEventArgs { OldInstrumentId = _instrumentId, Security = this };
                    _instrumentId = value;
                    InstrumentIdChanged?.Invoke(this, args);
                }
            }
        }
    }

    public class SecurityRepository
    {
        private Dictionary<string, HashSet<Security>> _securities = new Dictionary<string, HashSet<Security>>();

        public SecurityRepository(Security[] securities)
        {
            LoadSecurities(securities);
        }

        private void LoadSecurities(Security[] securities)
        {
            foreach (var security in securities)
            {
                if (!_securities.TryGetValue(security.InstrumentId, out var securityList))
                {
                    securityList = new HashSet<Security>();
                    _securities.Add(security.InstrumentId, securityList);
                }
                security.InstrumentIdChanged += OnSecurityInstrumentIdChanged;
                securityList.Add(security);
            }
        }

        private void OnSecurityInstrumentIdChanged(object sender, SecurityInstrumentIdChangedEventArgs e)
        {
            //it has to be there, if not, can be TOCTTOU (race of time issue), which we won't consider here, SO SAD :(
            if (_securities.TryGetValue(e.OldInstrumentId, out var oldSecurityList))
            {
                //even it does not exist, we don't care, because it won't throw exception :)
                oldSecurityList.Remove(e.Security);
            }

            if (!_securities.TryGetValue(e.Security.InstrumentId, out var newSecurityList))
            {
                newSecurityList = new HashSet<Security>();
                _securities.Add(e.Security.InstrumentId, newSecurityList);
            }
            newSecurityList.Add(e.Security);
        }

        public IEnumerable<Security> GetSecurities(string instrumentId)
        {
            if (_securities.TryGetValue(instrumentId, out var matchedSecurites))
                return matchedSecurites;
            return new Security[0];
        }
    }
}
