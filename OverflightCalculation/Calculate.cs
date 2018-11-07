using Newtonsoft.Json;
using OverflightCalculation.SOAPICCS;
using RestSharp;
using RestSharp.Authenticators;
using RightNow.AddIns.AddInViews;
using RightNow.AddIns.Common;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Windows.Forms;
using JsonKmCost;

namespace OverflightCalculation
{
    public partial class Calculate : UserControl
    {
        private bool inDesignMode;
        private IRecordContext recordContext;
        private IGlobalContext globalContext;
        private IGenericObject SENEAMOvers;
        private IIncident Incident;
        private IList<IGenericField> fields { get; set; }
        private RightNowSyncPortClient clientRN;

        private int Km { get; set; }
        private double KmPrice { get; set; }

        private string Departure { get; set; }
        private string Destine { get; set; }
        private int IDDeparture { get; set; }
        private int IDDestine { get; set; }
        private double Amount { get; set; }
        private int idTail { get; set; }
        private string Tail { get; set; }
        private string ICAODesignator { get; set; }

        private int OverTypeId { get; set; }
        private string OverType { get; set; }

        private DateTime start { get; set; }
        private DateTime end { get; set; }
        private int minutes { get; set; }


        public Calculate()
        {
            InitializeComponent();
        }

        public Calculate(bool inDesignMode, IRecordContext recordContext, IGlobalContext globalContext) : this()
        {
            this.inDesignMode = inDesignMode;
            this.recordContext = recordContext;
            this.globalContext = globalContext;
        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            try
            {
                if (Init())
                {
                    getOverType();
                    getAirports();
                    getTail();
                    Departure = getAirport(IDDeparture);
                    Destine = getAirport(IDDestine);

                    //MessageBox.Show("OverType: " + OverType);
                    if (OverType == "OVERFLIGHT")
                    {
                        if (IDDeparture != 0 && IDDestine != 0 && idTail != 0)
                        {
                            Km = getKilometers(Departure, Destine);
                            //MessageBox.Show("Km: " + Km.ToString());
                            Tail = getTailName(idTail);
                            //MessageBox.Show("Tail: " + Tail.ToString());
                            //MessageBox.Show("ICAODesignator: " + ICAODesignator.ToString());
                            KmPrice = getKmPrices(ICAODesignator);
                            //MessageBox.Show("Km: " + KmPrice.ToString());
                            Amount = Km * KmPrice;
                            //MessageBox.Show("Amount: " + Amount.ToString());
                            if (Km != 0)
                            {
                                foreach (IGenericField genField in fields)
                                {
                                    if (genField.Name == "Time")
                                    {
                                        genField.DataValue.Value = Km;
                                    }
                                    if (genField.Name == "Cost")
                                    {
                                        genField.DataValue.Value = KmPrice;
                                    }
                                    if (genField.Name == "Amount")
                                    {
                                        genField.DataValue.Value = Amount;
                                    }
                                }
                            }
                            else
                            {
                                foreach (IGenericField genField in fields)
                                {
                                    if (genField.Name == "Time")
                                    {
                                        genField.DataValue.Value = 0;
                                    }
                                    if (genField.Name == "Cost")
                                    {
                                        genField.DataValue.Value = 0;
                                    }
                                    if (genField.Name == "Amount")
                                    {
                                        genField.DataValue.Value = 0;
                                    }
                                }
                            }
                        }
                    }

                    if (OverType == "OVERTIME")
                    {
                        getDates();
                        //MessageBox.Show("Se obtuvieron fechas");
                        if (start != null && end != null && IDDeparture != 0)
                        {
                            minutes = GetMinutesLeg();
                            Tail = getTailName(idTail);
                            //MessageBox.Show("Tail: " + Tail.ToString());
                            //MessageBox.Show("Departure: " + Departure.ToString());
                            KmPrice = getMinPrices(Departure);
                            //MessageBox.Show("MinPrice: " + KmPrice.ToString());
                            Amount = minutes * KmPrice;
                            //MessageBox.Show("Amount: " + Amount.ToString());
                            foreach (IGenericField genField in fields)
                            {
                                if (genField.Name == "Cost")
                                {
                                    genField.DataValue.Value = KmPrice;
                                }
                            }
                            if (minutes != 0)
                            {
                                foreach (IGenericField genField in fields)
                                {
                                    if (genField.Name == "Time")
                                    {
                                        genField.DataValue.Value = minutes;
                                    }
                                    if (genField.Name == "Amount")
                                    {
                                        genField.DataValue.Value = Amount;
                                    }
                                }
                            }
                            else
                            {
                                foreach (IGenericField genField in fields)
                                {
                                    if (genField.Name == "Time")
                                    {
                                        genField.DataValue.Value = 0;
                                    }
                                    if (genField.Name == "Amount")
                                    {
                                        genField.DataValue.Value = 0;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Click: " + ex.Message + "Det" + ex.StackTrace);
            }
        }

        internal void LoadData()
        {
            try
            {
                Incident = (IIncident)recordContext.GetWorkspaceRecord(WorkspaceRecordType.Incident);
                SENEAMOvers = recordContext.GetWorkspaceRecord("CO$SENEAMOvers") as IGenericObject;
                if (Incident != null && SENEAMOvers != null)
                {
                    // MessageBox.Show(Incident.ID.ToString());
                    // MessageBox.Show(SENEAMOvers.Id.ToString());
                    fields = SENEAMOvers.GenericFields;
                }
                else
                {
                    btnCalculate.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load: " + ex.Message + "Det" + ex.StackTrace);
            }

        }
        internal void getAirports()
        {
            try
            {
                //MessageBox.Show("Obteniendo Aeropuertos");
                foreach (IGenericField genField in fields)
                {
                    if (genField.Name == "Departure")
                    {
                        IDDeparture = Convert.ToInt32(genField.DataValue.Value);
                        //MessageBox.Show("Departure: " + IDDeparture);
                    }
                    if (genField.Name == "Destination")
                    {
                        IDDestine = Convert.ToInt32(genField.DataValue.Value);
                        //MessageBox.Show("Destination: " + IDDestine);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetAirports: " + ex.Message + "Det" + ex.StackTrace);
            }
        }
        internal void getDates()
        {
            try
            {
                //MessageBox.Show("Obteniendo Aeropuertos");
                foreach (IGenericField genField in fields)
                {

                    if (genField.Name == "OperationDatetime")
                    {
                        start = DateTime.Parse(genField.DataValue.Value.ToString());
                        //MessageBox.Show("OperationDatetime: " + start.ToString());
                    }
                    if (genField.Name == "EndDateTime")
                    {
                        end = DateTime.Parse(genField.DataValue.Value.ToString());
                        //MessageBox.Show("EndDateTime: " + end.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetDates: " + ex.Message + "Det" + ex.StackTrace);
            }
        }
        internal void getOverType()
        {
            try
            {
                //MessageBox.Show("Obteniendo OverType");
                foreach (IGenericField genField in fields)
                {
                    if (genField.Name == "Type")
                    {
                        OverTypeId = Convert.ToInt32(genField.DataValue.Value);
                        //MessageBox.Show("OverType: " + OverType);
                    }
                }
                if (OverTypeId == 1)
                {
                    OverType = "OVERTIME";
                }
                if (OverTypeId == 2)
                {
                    OverType = "OVERFLIGHT";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("GetOverType: " + ex.Message + "Det" + ex.StackTrace);
            }
        }
        internal void getTail()
        {
            try
            {
                foreach (IGenericField genField in fields)
                {
                    if (genField.Name == "Tail")
                    {
                        idTail = Convert.ToInt32(genField.DataValue.Value);
                        //MessageBox.Show("Tail: " + idTail);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("getTail: " + ex.Message + "Det" + ex.StackTrace);
            }
        }
        internal int getKilometers(string Departure, string Destination)
        {
            try
            {
                int km = 0;
                var client = new RestClient("https://iccs.bigmachines.com/");
                client.Authenticator = new HttpBasicAuthenticator("servicios", "Sinergy*2018");
                string definicion = "?q={\"str_icao_iata\":{$like:\"" + Departure + "/" + Destine + "\"}}";
                globalContext.LogMessage("getKM: " + definicion);
                var request = new RestRequest("rest/v6/customRutas/" + definicion, Method.GET);
                IRestResponse response = client.Execute(request);
                RootObject rootObject = JsonConvert.DeserializeObject<RootObject>(response.Content);
                if (rootObject.items.Count > 0)
                {
                    foreach (Item i in rootObject.items)
                    {
                        km = i.int_km;
                    }
                }
                return km;
            }
            catch (Exception ex)
            {
                MessageBox.Show("getKilometers: " + ex.Message + "Det" + ex.StackTrace);
                return 0;
            }
        }

        internal double getMinPrices(string Departure)
        {
            try
            {
                string arrival = "IO_AEREO_" + Departure.Replace("-", "_").Trim();
                double price = 0;
                var client = new RestClient("https://iccs.bigmachines.com/");
                client.Authenticator = new HttpBasicAuthenticator("servicios", "Sinergy*2018");
                string definicion = "?totalResults=true&q={str_item_number:'OSSEIAS0185',str_icao_iata_code:'" + arrival + "'}&fields=flo_cost";
                globalContext.LogMessage("getMinPrices: " + definicion);
                var request = new RestRequest("rest/v6/customCostos/" + definicion, Method.GET);
                IRestResponse response = client.Execute(request);
                RootObject rootObject = JsonConvert.DeserializeObject<RootObject>(response.Content);
                if (rootObject.items.Count > 0)
                {
                    foreach (Item i in rootObject.items)
                    {
                        price = Convert.ToDouble(i.flo_cost);
                    }
                }
                return price;
            }
            catch (Exception ex)
            {
                MessageBox.Show("getMinPrices: " + ex.Message + "Det" + ex.StackTrace);
                return 0;
            }
        }
        internal double getKmPrices(string Icao)
        {
            try
            {
                double price = 0;
                var client = new RestClient("https://iccs.bigmachines.com/");
                client.Authenticator = new HttpBasicAuthenticator("servicios", "Sinergy*2018");
                string definicion = "?totalResults=false&q={str_aircraft_type:'" + Icao.ToString() + "',str_item_number:'AFVLEAP257'}&fields=flo_cost";
                globalContext.LogMessage("getKmPrices: " + definicion);
                var request = new RestRequest("rest/v6/customCostos/" + definicion, Method.GET);
                IRestResponse response = client.Execute(request);
                JsonKmCost.RootObject rootObjectKm = JsonConvert.DeserializeObject<JsonKmCost.RootObject>(response.Content);
                if (rootObjectKm.items.Count > 0)
                {
                    foreach (JsonKmCost.Item i in rootObjectKm.items)
                    {
                        price = Convert.ToDouble(i.flo_cost);
                    }
                }
                return price;
            }
            catch (Exception ex)
            {
                MessageBox.Show("getKmPrices: " + ex.Message + "Det" + ex.StackTrace);
                return 0;
            }
        }
        public bool Init()
        {
            try
            {
                bool result = false;
                EndpointAddress endPointAddr = new EndpointAddress(globalContext.GetInterfaceServiceUrl(ConnectServiceType.Soap));
                // Minimum required
                BasicHttpBinding binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential);
                binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;
                binding.ReceiveTimeout = new TimeSpan(0, 10, 0);
                binding.MaxReceivedMessageSize = 1048576; //1MB
                binding.SendTimeout = new TimeSpan(0, 10, 0);
                // Create client proxy class
                clientRN = new RightNowSyncPortClient(binding, endPointAddr);
                // Ask the client to not send the timestamp
                BindingElementCollection elements = clientRN.Endpoint.Binding.CreateBindingElements();
                elements.Find<SecurityBindingElement>().IncludeTimestamp = false;
                clientRN.Endpoint.Binding = new CustomBinding(elements);
                // Ask the Add-In framework the handle the session logic
                globalContext.PrepareConnectSession(clientRN.ChannelFactory);
                if (clientRN != null)
                {
                    result = true;
                }

                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Init: " + ex.Message + "Det" + ex.StackTrace);
                return false;
            }
        }
        public string getAirport(int IdAirport)
        {
            try
            {
                string IATACODE = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT LookupName FROM CO.Airports WHERE ID =" + IdAirport;
                clientRN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        IATACODE = data;
                    }
                }
                return IATACODE;
            }
            catch (Exception ex)
            {
                MessageBox.Show("getAirport: " + ex.Message + "Det" + ex.StackTrace);
                return "";

            }
        }
        public string getTailName(int IdTail)
        {
            try
            {
                string tail = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT LookupName,AircraftType1 FROM CO.Aircraft WHERE ID =" + IdTail;
                clientRN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Char delimiter = '|';
                        String[] substrings = data.Split(delimiter);
                        tail = substrings[0];
                        ICAODesignator = getICAODesi(Convert.ToInt32(substrings[1]));
                    }
                }
                return tail;
            }
            catch (Exception ex)
            {
                MessageBox.Show("getTailName: " + ex.Message + "Det" + ex.StackTrace);
                return "";
            }
        }
        public string getICAODesi(int IdAircraftType)
        {
            try
            {
                string Icao = "";
                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                APIAccessRequestHeader aPIAccessRequest = new APIAccessRequestHeader();
                clientInfoHeader.AppID = "Query Example";
                String queryString = "SELECT ICAODesignator FROM CO.AircraftType WHERE ID = " + IdAircraftType;
                clientRN.QueryCSV(clientInfoHeader, aPIAccessRequest, queryString, 10000, "|", false, false, out CSVTableSet queryCSV, out byte[] FileData);
                foreach (CSVTable table in queryCSV.CSVTables)
                {
                    String[] rowData = table.Rows;
                    foreach (String data in rowData)
                    {
                        Icao = data;
                    }
                }

                return Icao;
            }
            catch (Exception ex)
            {
                MessageBox.Show("getICAODesi: " + ex.Message + "Det" + ex.StackTrace);
                return "";
            }

        }
        public int GetMinutesLeg()
        {
            try
            {
                int minutes = 0;
                TimeSpan span = end.Subtract(start);
                MessageBox.Show("minutos: " + span.TotalMinutes.ToString());
                minutes = Convert.ToInt32(span.TotalMinutes);
                return minutes;
            }
            catch (Exception ex)
            {
                globalContext.LogMessage("GetMinutesLeg: " + ex.Message + "Det: " + ex.StackTrace);
                return 0;
            }
        }

    }

    public class Item
    {
        public int id { get; set; }
        public int int_km { get; set; }
        public int bol_int_active { get; set; }
        public int int_id_iccs { get; set; }
        public string str_icao_iata { get; set; }
        public double flo_cost { get; set; }
        public double flo_amount { get; set; }
        public object str_end_date { get; set; }
        public string str_start_date { get; set; }
        public List<Link> links { get; set; }
    }

    public class Link2
    {
        public string rel { get; set; }
        public string href { get; set; }
    }

    public class RootObject
    {
        public bool hasMore { get; set; }
        public List<Item> items { get; set; }
        public List<Link2> links { get; set; }
    }
    public class Link
    {
        public string rel { get; set; }
        public string href { get; set; }
    }
}
