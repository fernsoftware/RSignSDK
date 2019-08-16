using CrystalDecisions.Shared;
using RSignSDK;
using RSignSDK.Models;
using RSignSDK.Models.Authentication;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RSignTestForm
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void BtnSendPdfs_Click(object sender, EventArgs e)
        {
            try
            {
                TestReport oRpt = new TestReport();

                var credentials = new RSignAPICredentials("aaron.cullen@fernsoftware.com", "Whatever1!", "OQAxADgAQwBFADYAOQBEA");
                var options = new RSignAPIOptions
                {
                    CultureInfo = "en-us",
                };

                var file = @"C:\Users\James\Desktop\Work\MOZY Datasheet-ReportsAPI.pdf";
                using (var api = new RSignAPI(credentials, options))
                {
                    var envelopeId = api.Send(
                        file,
                        "Loan Contract.pdf",
                        "Template_Test",
                        "james.oreilly@fernsoftware.com",
                        "Mr. James",
                        "Have a loan on us",
                        "An email body :o",
                        "60 Days");

                    Console.WriteLine($"The signing request has been sent and responded with the identifier {envelopeId}");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            try
            {
                TestReport oRpt = new TestReport();

                var credentials = new RSignAPICredentials("aaron.cullen@fernsoftware.com", "Whatever1!", "OQAxADgAQwBFADYAOQBEA");
                var options = new RSignAPIOptions
                {
                    CultureInfo = "en-us"
                };

                var expiryType = "60 Days";
                var docNum = 1;
                //var emailBody = $@"<html><head><meta http - equiv = ""Content-Type"" content = ""text/html; charset=us-ascii""></head>
                // var emailBody = $@"<body><div><p style=""font - size:18px;"">This esignature document expires in <b>{expiryType}</b></p></div></body></html>";
                // var emailBody = $@"<body><div><p style=""font-size:48px"">This esignature document expires in <b>{expiryType}</b></p></div></body>";
                var emailBody = $@"<body><div><p style=""font-size:32px"">You have <b>{docNum}</b> document(s) to sign.</p>
                    <p style=""font-size:32px"">This e-signature request expires in <b>{expiryType}</b></p></div></body>";

                //var emailBody = string.Concat("This esignature document expires in ", "60 Days");

                var documentSend = new List<DocumentSend>();
                using (var s = oRpt.ExportToStream(ExportFormatType.PortableDocFormat))
                {
                    using (var reader = new BinaryReader(s))
                    {
                        var byteDoc = reader.ReadBytes(Convert.ToInt32(reader.BaseStream.Length));
                        documentSend.Add(
                            new DocumentSend
                            {
                                DocumentBytes = byteDoc,
                                DocumentName = "Contacts.pdf"
                            }
                       );
                    }
                }

                using (var api = new RSignAPI(credentials, options))
                {
                    var envelopeId = api.Send(
                        documentSend,
                        "Integration_Test",
                        "james.oreilly@fernsoftware.com",
                        "Mr. James",
                        "Have a loan on us",
                        emailBody,
                        expiryType);

                    Console.WriteLine($"The signing request has been sent and responded with the identifier {envelopeId}");
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void BtnGetStatus_Click(object sender, EventArgs e)
        {
            try
            {
                var credentials = new RSignAPICredentials("aaron.cullen@fernsoftware.com", "Whatever1!", "OQAxADgAQwBFADYAOQBEA");
                var options = new RSignAPIOptions
                {
                    CultureInfo = "en-us",
                };

                using (var api = new RSignAPI(credentials, options))
                {
                    var status = api.GetEnvelopeStatus(TxtEnvelopeDisplayCode.Text);

                    txtStatus.Text = status.Message;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                var credentials = new RSignAPICredentials("aaron.cullen@fernsoftware.com", "Whatever1!", "OQAxADgAQwBFADYAOQBEA");
                var options = new RSignAPIOptions
                {
                    CultureInfo = "en-us",
                };

                using (var api = new RSignAPI(credentials, options))
                {
                    var statusInfo = api.GetEnvelopeStatus(TxtEnvelopeDisplayCode.Text);
                    var completed = api.CancelEnvelope(statusInfo.EnvelopeId);
                    var status = api.GetEnvelopeStatus(TxtEnvelopeDisplayCode.Text);

                    txtStatus.Text = status.Message;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
