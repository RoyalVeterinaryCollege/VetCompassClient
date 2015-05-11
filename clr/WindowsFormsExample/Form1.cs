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

            var client = new CodingSessionFactory(Guid.NewGuid(), "not very secret", new Uri("http://10.144.3.72:5000/api/1.0/session/")); //new Uri("https://venomcoding.herokuapp.com/api/1.0/session/"));
            _session = client.StartCodingSession(new CodingSubject { CaseNumber = "winforms testing case" });
            //_session = client.ResumeCodingSession(new CodingSubject {CaseNumber = "winforms testing case"},(Guid) new GuidConverter().ConvertFromString("5537ade0-b91d-11e4-9f5c-0800200c9a66"));
            _source.AllowEdit = false;
            _source.AllowNew = false;
            lstBox.DisplayMember = "Name";
            lstBox.DataSource = _source;
        }

        private void txtQuery_KeyUp(object sender, KeyEventArgs e)
        {
            var text = txtQuery.Text;
            if (text == null) return;//guard against searching on empty, null, or whitespace strings
            if (String.IsNullOrEmpty(text.Trim())) 
            { 
                _source.Clear();
                return; 
            }
            if (_session.IsFaulted) //guard faults
            {
                var error = _session.ServerErrorMessage ?? _session.Exception.ToString();
                MessageBox.Show(error, "Server error message");
                return;
            }

            //call asynchronously to the webservice, to keep the UI responsive but..
            var task = _session.QueryAsync(new VeNomQuery(text));
 
            //..in a winforms/wpf app you will need to do the UI update on the UI thread
            //this is done using the TaskScheduler.FromCurrentSynchronizationContext() call
            task.ContinueWith(HandleTaskResult, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void HandleTaskResult(Task<VeNomQueryResponse> task)
        {
            switch (task.Status)
            {
                case TaskStatus.Canceled: //handle cancellation
                    return;
                case TaskStatus.Faulted:
                    //you can access the task.Exception here
                    //after any task exception, the coding session will be faulted (IsFaulted == true) and you can't contine to use it
                    //If possible, a serverside error message will additionally be placed in _session.ServerErrorMessage
                    var error = _session.ServerErrorMessage ?? task.Exception.ToString();
                    MessageBox.Show(error, "Error"); //implement proper exception handling
                    return;
            }
            var result = task.Result; //handle success
            BindResults(result);
        }

        /// <summary>
        /// Binds the query response to the UI
        /// </summary>
        /// <param name="queryResponse"></param>
        private void BindResults(VeNomQueryResponse queryResponse)
        {
            if (txtQuery.Text != queryResponse.Query.SearchExpression) return; //guard against multiple queries in quick succession coming out of order

            _source.Clear();
            foreach (var vetCompassCode in queryResponse.Results)
            {
                _source.Add(vetCompassCode);
            }
        }
    }

}
