using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using Newtonsoft.Json;

using RSignSDK.Contracts;
using RSignSDK.Http;
using RSignSDK.Models;
using RSignSDK.Models.Authentication;
using RSignSDK.Models.MasterData;
using static RSignSDK.Contracts.Enumerations;

namespace RSignSDK
{
    /// <summary>
    /// Implementation for accessing RSign API.
    /// </summary>
    public class RSignAPI : IRSignAPI, IRSignAPIInternal
    {
        private HashSet<EnvelopeType> _envelopeTypes;
        private DateFormat _dateFormat;
        private ExpiryType _expiryType;

        private readonly string _ipAddress;

        private readonly RSignHttpClient _httpClient;
        private readonly RSignAPICredentials _credentials;
        private readonly RSignAPIOptions _options;

        private const string ProductionApiUrl = "https://webapi.rsign.com/api/V1/";

        /// <summary>
        /// Constructs RSignAPI with credentials and default options.
        /// </summary>
        /// <param name="credentials">Your RSign API credentials.</param>
        public RSignAPI(RSignAPICredentials credentials)
            : this(credentials, new RSignAPIOptions())
        { }

        /// <summary>
        /// Constructs RSignAPI with credentials and options.
        /// </summary>
        /// <param name="credentials">Your RSign API credentials.</param>
        /// <param name="options">Your custom RSign API options.</param>
        public RSignAPI(RSignAPICredentials credentials, RSignAPIOptions options)
        {
            _credentials = credentials;
            _options = options;

            _httpClient = new RSignHttpClient(ProductionApiUrl);

            _ipAddress = GetComputerIPAddress();
        }

        /// <summary>
        /// Indicates whether this instance has been successfully authenticated.
        /// </summary>
        public bool IsAuthenticated { get; private set; }

        private void Authenticate()
        {
            var response = _httpClient
                .Post("Authentication/AuthenticateUser", JsonConvert.SerializeObject(_credentials));

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new AuthenticationException("Invalid RSign API user name or password");
            }

            IsAuthenticated = true;

            var authenticationResponse = JsonConvert
                    .DeserializeObject<AuthenticationResponse>(response.Content.ReadAsStringAsync().Result);

            _httpClient.SetAuthenticationToken(authenticationResponse.AuthToken);

            _envelopeTypes = new HashSet<EnvelopeType>(GetEnvelopeTypes());

            _dateFormat = GetDateFormats()
                .Single(x => _options.DateFormat.Equals(x.Description, StringComparison.InvariantCultureIgnoreCase));

            _expiryType = GetExpiryTypes()
                .Single(x => _options.ExpiryType.Equals(x.Description, StringComparison.InvariantCultureIgnoreCase));
        }

        public SendEnvelopeResponse Send(List<DocumentSend> documentSend, string templateName, string recipientEmail, string recipientName, string subject, string body, string expiryType, int reminder1 = 0, int reminder2 = 0,
            AccessAutenticationTypes accessAutenticationType = AccessAutenticationTypes.Select, string password = "")
        {
            _expiryType = GetExpiryTypes()
                .SingleOrDefault(x => expiryType.Equals(x.Description, StringComparison.InvariantCultureIgnoreCase));

            if (_expiryType == null)
            {
                throw new Exception("Expiry option can not be found.");
            }

            var initializeEnvelopeResponse = InitializeEnvelope(new InitializeEnvelopeRequest());
            var templates = GetTemplates();
            var template = templates.SingleOrDefault(x => x.Name == templateName);

            if (template == null)
            {
                throw new Exception("Esignature template can not be found.");
            }

            var useTemplateResponse = UseTemplate(new UseTemplateRequest
            {
                TemplateID = template.ID,
                DocID = initializeEnvelopeResponse.EnvelopeID
            });

            foreach (var doc in documentSend)
            {
                var uploadLocalDocument = UploadLocalDocument(new UploadLocalDocumentRequest(doc.DocumentBytes)
                {
                    FileName = doc.DocumentName,
                    EnvelopeID = useTemplateResponse.EnvelopeID,
                    EnvelopeStage = "InitializeUseTemplate"
                });
            }

            var myReq = "";
            var signer = useTemplateResponse.EnvelopeDetails.RecipientList.Where(x => x.RecipientType == "Signer");
            foreach (var recipient in signer)
            {
                var addUpdateRecipientResponse = AddUpdateRecipient(new AddUpdateRecipientRequest
                {
                    RecipientID = recipient.ID,
                    EnvelopeID = useTemplateResponse.EnvelopeID,
                    RecipientType = recipient.RecipientTypeID,
                    RecipientName = recipientName,
                    Email = recipientEmail,
                    Order = 1
                });

                myReq = addUpdateRecipientResponse.EnvelopeID;
            }

            var prepareEnvelopeRequest = new PrepareEnvelopeRequest
            {
                EnvelopeID = myReq,
                Message = body,
                Subject = subject,
            };

            if (accessAutenticationType == AccessAutenticationTypes.Endtoend)
            {
                prepareEnvelopeRequest.AccessAuthType = "fc14f65b-6fe9-4211-90fb-a03f241a55be";
                prepareEnvelopeRequest.AccessAuthenticationPassword = password;
                prepareEnvelopeRequest.PasswordRequiredToSign = true;
                prepareEnvelopeRequest.PasswordRequiredToOpen = true;
                prepareEnvelopeRequest.PasswordToSign = password; // Sign document
                prepareEnvelopeRequest.PasswordToOpen = password; // Open signed document
                prepareEnvelopeRequest.IsRandomPassword = false;
                prepareEnvelopeRequest.IsPasswordMailToSigner = false;
            }
            else if (accessAutenticationType == AccessAutenticationTypes.RequiredToOpenSigned)
            {
                prepareEnvelopeRequest.AccessAuthType = "5d8162da-c7a7-40be-9cf1-442fb2524ca3";
                prepareEnvelopeRequest.AccessAuthenticationPassword = password;
                prepareEnvelopeRequest.PasswordRequiredToSign = true;
                prepareEnvelopeRequest.PasswordToSign = password; // Sign document
                prepareEnvelopeRequest.IsRandomPassword = false;
                prepareEnvelopeRequest.IsPasswordMailToSigner = false;
            }

            var prepareEnvelopeResponse = PrepareEnvelope(prepareEnvelopeRequest);

            var sendEnvelopeResponse = SendEnvelope(new SendEnvelopeRequest
            {
                EnvelopeID = prepareEnvelopeResponse.EnvelopeId,
                UserID = useTemplateResponse.EnvelopeDetails.RecipientList.Single(x => x.RecipientType == "Sender").ID,
                EnvelopeTypeID = useTemplateResponse.EnvelopeTypeID
            });

            return sendEnvelopeResponse;
        }

        public SendEnvelopeResponse Send(string filePath, string documentName, string templateName, string recipientEmail, string recipientName, string subject, string body, string expiryType, int reminder1 = 0, int reminder2 = 0,
            AccessAutenticationTypes accessAutenticationType = AccessAutenticationTypes.Select, string password = "")
        {
            var envelopeId = "";

            var initializeEnvelopeResponse = InitializeEnvelope(new InitializeEnvelopeRequest());


            var templates = GetTemplates();

            var template = templates.SingleOrDefault(x => x.Name == templateName);
            if (template == null)
            {
                throw new Exception("Esignature template can not be found.");
            }

            var useTemplateResponse = UseTemplate(new UseTemplateRequest
            {
                TemplateID = template.ID,
                DocID = initializeEnvelopeResponse.EnvelopeID
            });

            var bytesDoc = System.IO.File.ReadAllBytes(filePath);
            var uploadLocalDocument = UploadLocalDocument(new UploadLocalDocumentRequest(bytesDoc)
            {
                FileName = documentName,
                EnvelopeID = useTemplateResponse.EnvelopeID,
                EnvelopeStage = "InitializeUseTemplate"
            });

            var myReq = "";
            var signer = useTemplateResponse.EnvelopeDetails.RecipientList.Where(x => x.RecipientType == "Signer");
            foreach (var recipient in signer)
            {
                var addUpdateRecipientResponse = AddUpdateRecipient(new AddUpdateRecipientRequest
                {
                    RecipientID = recipient.ID,
                    EnvelopeID = useTemplateResponse.EnvelopeID,
                    RecipientType = recipient.RecipientTypeID,
                    RecipientName = recipientName,
                    Email = recipientEmail,
                    Order = 1
                });

                myReq = addUpdateRecipientResponse.EnvelopeID;
            }

            var prepareEnvelopeRequest = new PrepareEnvelopeRequest
            {
                EnvelopeID = myReq,
                Message = body,
                Subject = subject,
            };

            if (accessAutenticationType == AccessAutenticationTypes.Endtoend)
            {
                prepareEnvelopeRequest.AccessAuthType = "fc14f65b-6fe9-4211-90fb-a03f241a55be";
                prepareEnvelopeRequest.AccessAuthenticationPassword = password;
                prepareEnvelopeRequest.PasswordRequiredToSign = true;
                prepareEnvelopeRequest.PasswordRequiredToOpen = true;
                prepareEnvelopeRequest.PasswordToSign = password; // Sign document
                prepareEnvelopeRequest.PasswordToOpen = password; // Open signed document
                prepareEnvelopeRequest.IsRandomPassword = false;
                prepareEnvelopeRequest.IsPasswordMailToSigner = false;
            }
            else if (accessAutenticationType == AccessAutenticationTypes.RequiredToOpenSigned)
            {
                prepareEnvelopeRequest.AccessAuthType = "5d8162da-c7a7-40be-9cf1-442fb2524ca3";
                prepareEnvelopeRequest.AccessAuthenticationPassword = password;
                prepareEnvelopeRequest.PasswordRequiredToSign = true;
                prepareEnvelopeRequest.PasswordToSign = password; // Sign document
                prepareEnvelopeRequest.IsRandomPassword = false;
                prepareEnvelopeRequest.IsPasswordMailToSigner = false;
            }

            var prepareEnvelopeResponse = PrepareEnvelope(prepareEnvelopeRequest);

            //var prepareEnvelopeResponse = PrepareEnvelope(new PrepareEnvelopeRequest
            //{
            //    EnvelopeID = myReq,
            //    Message = body,
            //    Subject = subject,
            //    SendReminderIn = reminder1,
            //    ThenSendReminderIn = reminder2
            //});

            var sendEnvelopeResponse = SendEnvelope(new SendEnvelopeRequest
            {
                EnvelopeID = prepareEnvelopeResponse.EnvelopeId,
                UserID = useTemplateResponse.EnvelopeDetails.RecipientList.Single(x => x.RecipientType == "Sender").ID,
                EnvelopeTypeID = useTemplateResponse.EnvelopeTypeID
            });

            envelopeId = sendEnvelopeResponse.EnvelopeCode;

            return sendEnvelopeResponse;
        }

        public EnvelopeStatusResponse GetEnvelopeStatus(string envelopeDisplayCode)
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get($"Envelope/GetEnvelopeStatus/{envelopeDisplayCode}");

            return JsonConvert
                .DeserializeObject<EnvelopeStatusResponse>(response.Content.ReadAsStringAsync().Result);
        }

        public DownloadSignedContractResponse DownloadSignedContract(string envelopeDisplayCode)
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get($"Manage/GetDownloadedTerminatedAndIncompleteExipredDocument/{envelopeDisplayCode}");

            return JsonConvert
                .DeserializeObject<DownloadSignedContractResponse>(response.Content.ReadAsStringAsync().Result);
        }

        public bool CancelEnvelope(string envelopeId)
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get($"Manage/CancelEnvelopeTrans/{envelopeId}");

            return response.StatusCode == HttpStatusCode.OK;
        }

        public bool DeleteFinalContract(string envelopeId)
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get($"Manage/DeleteFinalContract/{envelopeId}");

            return response.StatusCode == HttpStatusCode.OK;
        }

        /// <summary>
        /// Initializes a new envelope without recipients or templates attached.
        /// </summary>
        /// <param name="request">The parameters for the new envelope.</param>
        /// <returns>The response from the InitializeEnvelope API method, as returned by RSign.</returns>
        public InitializeEnvelopeResponse InitializeEnvelope(InitializeEnvelopeRequest request)
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            request.SetDateFormat(_dateFormat);

            //var x = new ExpiryType();
            //x.ID = new Guid("25b259e0-5c43-4724-b215-406aad6000c1");
            //x.Description = "1 Day";

            // request.SetExpiryType(x);
            request.SetExpiryType(_expiryType);
            request.SetIpAddress(_ipAddress);

            var response = _httpClient.Post("Envelope/InitializeEnvelope", JsonConvert.SerializeObject(request));

            return JsonConvert
                .DeserializeObject<InitializeEnvelopeResponse>(response.Content.ReadAsStringAsync().Result);
        }

        ///<summary>
        ///Need to upload local document from local machine
        ///</summary>
        ///<param name="request">The parameters for the preparation request.</param>
        /// <returns>The response from the PrepareEnvelope API method, as returned by RSign.</returns>
        public UploadLocalDocumentResponse UploadLocalDocument(UploadLocalDocumentRequest request)
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Post("Document/UploadLocalDocument", JsonConvert.SerializeObject(request));

            return JsonConvert
                .DeserializeObject<UploadLocalDocumentResponse>(response.Content.ReadAsStringAsync().Result);
        }

        /// <summary>
        /// Creates a template bound to an envelope without recipients attached.
        /// </summary>
        /// <param name="request">The parameters for the template.</param>
        /// <returns>The response from the UseTemplate API method, as returned by RSign.</returns>
        public UseTemplateResponse UseTemplate(UseTemplateRequest request)
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            request.SetIpAddress(_ipAddress);

            var response = _httpClient.Post("Envelope/UseTemplate", JsonConvert.SerializeObject(request));

            return JsonConvert
                .DeserializeObject<UseTemplateResponse>(response.Content.ReadAsStringAsync().Result);
        }

        /// <summary>
        /// Modifies the list of recipients on an envelope.
        /// </summary>
        /// <param name="request">The parameters for the recipient.</param>
        /// <returns>The response from the AddUpdateRecipient API method, as returned by RSign.</returns>
        public AddUpdateRecipientResponse AddUpdateRecipient(AddUpdateRecipientRequest request)
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Post("Envelope/AddUpdateRecipient", JsonConvert.SerializeObject(request));

            return JsonConvert
                .DeserializeObject<AddUpdateRecipientResponse>(response.Content.ReadAsStringAsync().Result);
        }

        /// <summary>
        /// Sets final sending parameters on an envelope.
        /// </summary>
        /// <param name="request">The parameters for the preparation request.</param>
        /// <returns>The response from the PrepareEnvelope API method, as returned by RSign.</returns>
        public PrepareEnvelopeResponse PrepareEnvelope(PrepareEnvelopeRequest request)
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            request.SetDateFormat(_dateFormat);
            request.SetExpiryType(_expiryType);
            request.SetCultureInfo(_options.CultureInfo);

            var response = _httpClient.Post("Envelope/PrepareEnvelope", JsonConvert.SerializeObject(request));

            return JsonConvert
                .DeserializeObject<PrepareEnvelopeResponse>(response.Content.ReadAsStringAsync().Result);
        }

        /// <summary>
        /// Sends a prepared envelope with attached template to a list of recipients.
        /// </summary>
        /// <param name="request">The parameters for sending the envelope.</param>
        /// <returns>The response from the SendEnvelope API method, as returned by RSign.</returns>
        public SendEnvelopeResponse SendEnvelope(SendEnvelopeRequest request)
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            request.SetIpAddress(_ipAddress);

            var response = _httpClient.Post("Envelope/SendEnvelope", JsonConvert.SerializeObject(request));

            return JsonConvert
                .DeserializeObject<SendEnvelopeResponse>(response.Content.ReadAsStringAsync().Result);
        }

        /// <summary>
        /// Returns the available templates.
        /// </summary>
        /// <returns>The response from the GetTemplates API method, as returned by RSign.</returns>
        public IEnumerable<Template> GetTemplates()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var envelopeType = _envelopeTypes.Single(x => x.Description.Equals("Template", StringComparison.InvariantCultureIgnoreCase));

            var response = _httpClient.Get(string.Format("Template/GetConsumableListForEnvelope/{0}", envelopeType.EnvelopeTypeId));

            return JsonConvert
                .DeserializeObject<TemplateList>(response.Content.ReadAsStringAsync().Result)
                .Templates
                .ToList();
        }

        /// <summary>
        /// Returns the available templates.
        /// </summary>
        /// <returns>The response from the GetTemplates API method, as returned by RSign.</returns>
        public IEnumerable<AccessAuthType> GetAuthTypes()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/AccessAuthType");

            return JsonConvert
                .DeserializeObject<MasterDataList<AccessAuthType>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available rules.
        /// </summary>
        /// <returns>The response from the GetRules API method, as returned by RSign.</returns>
        public IEnumerable<Rule> GetRules()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var envelopeType = _envelopeTypes.Single(x => x.Description == "TemplateRule");

            var response = _httpClient.Get(string.Format("Template/GetConsumableListForEnvelope/{0}", envelopeType.EnvelopeTypeId));

            return JsonConvert
                .DeserializeObject<RuleList>(response.Content.ReadAsStringAsync().Result)
                .Rules
                .ToList();
        }

        #region Master Data methods

        /// <summary>
        /// Returns the available controls.
        /// </summary>
        /// <returns>The response from the GetControls API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<Control> GetControls()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/CONTROLS");

            return JsonConvert
                .DeserializeObject<MasterDataList<Control>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available date formats.
        /// </summary>
        /// <returns>The response from the GetDateFormats API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<DateFormat> GetDateFormats()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/DATEFORMAT");

            return JsonConvert
                .DeserializeObject<MasterDataList<DateFormat>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available drop down options.
        /// </summary>
        /// <returns>The response from the GetDropDownOptions API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<DropDownOption> GetDropDownOptions()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/DROPDOWNOPTION");

            return JsonConvert
                .DeserializeObject<MasterDataList<DropDownOption>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available envelope types.
        /// </summary>
        /// <returns>The response from the GetEnvelopeTypes API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<EnvelopeType> GetEnvelopeTypes()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/ENVELOPETYPE");

            return JsonConvert
                .DeserializeObject<MasterDataList<EnvelopeType>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available expiry types.
        /// </summary>
        /// <returns>The response from the GetExpiryTypes API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<ExpiryType> GetExpiryTypes()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/EXPIRYTYPE");

            return JsonConvert
                .DeserializeObject<MasterDataList<ExpiryType>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available fonts.
        /// </summary>
        /// <returns>The response from the GetFonts API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<Font> GetFonts()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/FONT");

            return JsonConvert
                .DeserializeObject<MasterDataList<Font>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available mail templates.
        /// </summary>
        /// <returns>The response from the GetMailTemplates API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<MailTemplate> GetMailTemplates()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/MAILTEMPLATE");

            return JsonConvert
                .DeserializeObject<MasterDataList<MailTemplate>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available max characters.
        /// </summary>
        /// <returns>The response from the GetMaxCharacters API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<MaxCharacter> GetMaxCharacters()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/MAXCHARACTER");

            return JsonConvert
                .DeserializeObject<MasterDataList<MaxCharacter>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available recipient types.
        /// </summary>
        /// <returns>The response from the GetRecipientTypes API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<RecipientType> GetRecipientTypes()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/RECIPIENTTYPE");

            return JsonConvert
                .DeserializeObject<MasterDataList<RecipientType>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available RSign stages.
        /// </summary>
        /// <returns>The response from the GetRSignStages API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<RSignStage> GetRSignStages()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/RSIGNSTAGE");

            return JsonConvert
                .DeserializeObject<MasterDataList<RSignStage>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available rule configurations.
        /// </summary>
        /// <returns>The response from the GetRuleConfigurations API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<RuleConfiguration> GetRuleConfigurations()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/RULECONFIG");

            return JsonConvert
                .DeserializeObject<MasterDataList<RuleConfiguration>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available settings for types.
        /// </summary>
        /// <returns>The response from the GetSettingsForTypes API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<SettingsForType> GetSettingsForTypes()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/SETTINGSFORTYPE");

            return JsonConvert
                .DeserializeObject<MasterDataList<SettingsForType>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available settings key configurations.
        /// </summary>
        /// <returns>The response from the GetSettingsKeyConfigurations API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<SettingsKeyConfiguration> GetSettingsKeyConfigurations()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/SETTINGSKEYCONFIG");

            return JsonConvert
                .DeserializeObject<MasterDataList<SettingsKeyConfiguration>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available show settings tabs.
        /// </summary>
        /// <returns>The response from the GetShowSettingsTabs API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<ShowSettingsTab> GetShowSettingsTabs()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/SHOWSETTINGSTAB");

            return JsonConvert
                .DeserializeObject<MasterDataList<ShowSettingsTab>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available signature fonts.
        /// </summary>
        /// <returns>The response from the GetSignatureFonts API method, as returned by RSign.</returns>
        /// /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<string> GetSignatureFonts()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/SIGNATUREFONT");

            return JsonConvert
                .DeserializeObject<MasterDataList<string>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available signature types.
        /// </summary>
        /// <returns>The response from the GetSignatureTypes API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<SignatureType> GetSignatureTypes()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/SIGNATURETYPE");

            return JsonConvert
                .DeserializeObject<MasterDataList<SignatureType>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available sign font styles.
        /// </summary>
        /// <returns>The response from the GetSignFontStyles API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<SignFontStyle> GetSignFontStyles()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/SIGNFONTSTYLE");

            return JsonConvert
                .DeserializeObject<MasterDataList<SignFontStyle>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available status codes.
        /// </summary>
        /// <returns>The response from the GetStatusCodes API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<StatusCode> GetStatusCodes()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/STATUSCODE");

            return JsonConvert
                .DeserializeObject<MasterDataList<StatusCode>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available text types.
        /// </summary>
        /// <returns>The response from the GetTextTypes API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<TextType> GetTextTypes()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/TEXTTYPE");

            return JsonConvert
                .DeserializeObject<MasterDataList<TextType>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available time zones.
        /// </summary>
        /// <returns>The response from the GetTimeZones API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<Models.MasterData.TimeZone> GetTimeZones()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/TIMEZONE");

            return JsonConvert
                .DeserializeObject<MasterDataList<Models.MasterData.TimeZone>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available user constants.
        /// </summary>
        /// <returns>The response from the GetUserConstants API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<UserConstant> GetUserConstants()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/USERCONSTANTS");

            return JsonConvert
                .DeserializeObject<MasterDataList<UserConstant>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        /// <summary>
        /// Returns the available user types.
        /// </summary>
        /// <returns>The response from the GetUserTypes API method, as returned by RSign.</returns>
        /// <exception cref="AuthenticationException">This exception is thrown if the supplied credentials are invalid.</exception>
        public IEnumerable<UserType> GetUserTypes()
        {
            if (!IsAuthenticated)
            {
                Authenticate();
            }

            var response = _httpClient.Get("Dashboard/GetMasterData/USERTYPE");

            return JsonConvert
                .DeserializeObject<MasterDataList<UserType>>(response.Content.ReadAsStringAsync().Result)
                .MasterList
                .AsEnumerable();
        }

        #endregion Master Data methods

        private string GetComputerIPAddress()
        {
            string ipAddress = string.Empty;

            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    ipAddress = (socket.LocalEndPoint as IPEndPoint).Address.ToString();
                }
            }
            catch
            { }

            return ipAddress;
        }

        public void Dispose()
        {
            if (_httpClient != null)
            {
                _httpClient.Dispose();
            }
        }

        public IEnumerable<EnvelopeStatus> GetEnvelopeStatuses()
        {
            throw new NotImplementedException();
        }
    }
}