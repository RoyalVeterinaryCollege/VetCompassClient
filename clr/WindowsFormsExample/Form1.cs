using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using VetCompass.Client;

namespace WindowsFormsExample
{
    public partial class Form1 : Form
    {
        readonly ICodingSession _session;
        readonly BindingList<VetCompassCode> _source = new BindingList<VetCompassCode>();

        public Form1()
        {
            InitializeComponent();

            var client = new CodingSessionFactory(Guid.NewGuid(), "not very secret", new Uri("https://venomcoding.herokuapp.com/api/1.0/session/"));
            _session = client.StartCodingSession(new CodingSubject { CaseNumber = "winforms testing case" });
            _source.AllowEdit = false;
            _source.AllowNew = false;
            lstBox.DisplayMember = "Name";
            lstBox.DataSource = _source;
        }

        private void txtQuery_KeyUp(object sender, KeyEventArgs e)
        {
            var text = txtQuery.Text;
            if (String.IsNullOrWhiteSpace(text)) //guard against searching on the empty string
            { 
                _source.Clear();
                return; 
            }
            //call asynchronously to the webservice, to keep the UI responsive but..
            var task = _session.QueryAsync(new VeNomQuery(text)); 
            //..in a winforms/wpf app you will need to do the UI update on the UI thread
            //this is done using the TaskScheduler.FromCurrentSynchronizationContext() call
            task.ContinueWith(t => BindResults(t.Result), TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void BindResults(VeNomQueryResponse result)
        {
            if (txtQuery.Text != result.Query.SearchExpression) return; //guard against multiple queries in quick succession coming out of order
            
            _source.Clear();
            foreach (var vetCompassCode in result.Results)
            {
                _source.Add(vetCompassCode);
            }
        }
    }
}
