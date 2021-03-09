using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

using Altinn.App.Common.Enums;
using Altinn.App.Common.Models;
using Altinn.App.Services.Configuration;
using Altinn.App.Services.Helpers;
using Altinn.App.Services.Interface;
using Altinn.App.Services.Models;
using Altinn.App.Services.Models.Validation;
using Altinn.Common.EFormidlingClient;
using Altinn.Common.EFormidlingClient.Configuration;
using Altinn.Common.EFormidlingClient.Models;
using Altinn.Common.EFormidlingClient.Models.SBD;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Storage.Interface.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Altinn.App.Services.Implementation
{
    /// <summary>
    /// Default implementation of the core Altinn App interface.
    /// </summary>
    public abstract class AppBase : IAltinnApp
    {
        private readonly Application _appMetadata;
        private readonly IAppResources _resourceService;
        private readonly ILogger<AppBase> _logger;
        private readonly IData _dataService;
        private readonly IProcess _processService;
        private readonly IPDF _pdfService;
        private readonly IPrefill _prefillService;
        private readonly IInstance _instanceService;
        private readonly IRegister _registerService;
        private readonly string pdfElementType = "ref-data-as-pdf";
        private readonly UserHelper _userHelper;
        private readonly IProfile _profileService;
        private readonly IText _textService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOptions<EFormidlingClientSettings> _eSettings;

        private readonly IEFormidlingClient _eFormidlingClient;

        /// <summary>
        /// Initialize a new instance of <see cref="AppBase"/> class with the given services.
        /// </summary>
        /// <param name="resourceService">The service giving access to local resources.</param>
        /// <param name="logger">A logging service.</param>
        /// <param name="dataService">The service giving access to data.</param>
        /// <param name="processService">The service giving access the App process.</param>
        /// <param name="pdfService">The service giving access to the PDF generator.</param>
        /// <param name="prefillService">The service giving access to prefill mechanisms.</param>
        /// <param name="instanceService">The service giving access to instance data</param>
        /// <param name="registerService">The service giving access to register data</param>
        /// <param name="settings">the general settings</param>
        /// <param name="profileService">the profile service</param>
        /// <param name="textService">The text service</param>
        /// <param name="httpContextAccessor">the httpContextAccessor</param>
        /// <param name="eFormidlingClient">Test</param>
        /// <param name="eFormidlingSettings">Test settings</param>
        protected AppBase(
            IAppResources resourceService,
            ILogger<AppBase> logger,
            IData dataService,
            IProcess processService,
            IPDF pdfService,
            IPrefill prefillService,
            IInstance instanceService,
            IRegister registerService,
            IOptions<GeneralSettings> settings,
            IProfile profileService,
            IText textService,
            IHttpContextAccessor httpContextAccessor,
            IEFormidlingClient eFormidlingClient,
            IOptions<EFormidlingClientSettings> eFormidlingSettings)

        {
            _eSettings = eFormidlingSettings;
            _appMetadata = resourceService.GetApplication();
            _resourceService = resourceService;
            _logger = logger;
            _dataService = dataService;
            _processService = processService;
            _pdfService = pdfService;
            _prefillService = prefillService;
            _instanceService = instanceService;
            _registerService = registerService;
            _userHelper = new UserHelper(profileService, registerService, settings);
            _profileService = profileService;
            _textService = textService;
            _httpContextAccessor = httpContextAccessor;
            _eFormidlingClient = eFormidlingClient;

        }

        /// <inheritdoc />
        public abstract Type GetAppModelType(string dataType);

        /// <inheritdoc />
        public abstract object CreateNewAppModel(string dataType);

        /// <inheritdoc />
        public abstract Task<bool> RunAppEvent(AppEventType appEvent, object model, ModelStateDictionary modelState = null);

        /// <inheritdoc />
        public abstract Task RunDataValidation(object data, ModelStateDictionary validationResults);

        /// <inheritdoc />
        public abstract Task RunTaskValidation(Instance instance, string taskId, ModelStateDictionary validationResults);

        /// <inheritdoc />
        public abstract Task<bool> RunCalculation(object data);

        /// <inheritdoc />
        public abstract Task<InstantiationValidationResult> RunInstantiationValidation(Instance instance);

        /// <inheritdoc />
        public abstract Task RunDataCreation(Instance instance, object data);

        /// <inheritdoc />
        public abstract Task<AppOptions> GetOptions(string id, AppOptions options);

        /// <inheritdoc />
        public abstract Task RunProcessTaskEnd(string taskId, Instance instance);

        /// <inheritdoc />
        public abstract Task<LayoutSettings> FormatPdf(LayoutSettings layoutSettings, object data);

        /// <inheritdoc />
        public Task<string> OnInstantiateGetStartEvent()
        {
            _logger.LogInformation("OnInstantiate: GetStartEvent");

            // return start event
            return Task.FromResult("StartEvent_1");
        }

        /// <inheritdoc />
        public async Task OnStartProcess(string startEvent, Instance instance)
        {
            await Task.CompletedTask;
            _logger.LogInformation($"OnStartProcess for {instance.Id}");
        }

        /// <inheritdoc />
        public async Task OnEndProcess(string endEvent, Instance instance)
        {
            await Task.CompletedTask;
            _logger.LogInformation($"OnEndProcess for {instance.Id}, endEvent: {endEvent}");
        }

        /// <inheritdoc />
        public async Task OnStartProcessTask(string taskId, Instance instance)
        {
            _logger.LogInformation($"OnStartProcessTask for {instance.Id}");

            foreach (DataType dataType in _appMetadata.DataTypes.Where(dt => dt.TaskId == taskId && dt.AppLogic?.AutoCreate == true))
            {
                _logger.LogInformation($"Auto create data element: {dataType.Id}");

                DataElement dataElement = instance.Data.Find(d => d.DataType == dataType.Id);

                if (dataElement == null)
                {
                    dynamic data = CreateNewAppModel(dataType.AppLogic.ClassRef);

                    // runs prefill from repo configuration if config exists
                    await _prefillService.PrefillDataModel(instance.InstanceOwner.PartyId, dataType.Id, data);
                    await RunDataCreation(instance, data);
                    Type type = GetAppModelType(dataType.AppLogic.ClassRef);

                    DataElement createdDataElement = await _dataService.InsertFormData(instance, dataType.Id, data, type);
                    instance.Data.Add(createdDataElement);

                    _logger.LogInformation($"Created data element: {createdDataElement.Id}");
                }
            }
        }

        /// <inheritdoc />
        public async Task<bool> CanEndProcessTask(string taskId, Instance instance, List<ValidationIssue> validationIssues)
        {
            // check if the task is validated
            if (instance.Process?.CurrentTask?.Validated != null)
            {
                ValidationStatus validationStatus = instance.Process.CurrentTask.Validated;

                if (validationStatus.CanCompleteTask)
                {
                    return true;
                }
            }
            else
            {
                if (validationIssues.Count == 0)
                {
                    return true;
                }
            }

            return await Task.FromResult(false);
        }

        /// <inheritdoc />
        public async Task OnEndProcessTask(string taskId, Instance instance)
        {
            await RunProcessTaskEnd(taskId, instance);

            _logger.LogInformation($"OnEndProcessTask for {instance.Id}. Locking data elements connected to {taskId}");

            List<DataType> dataTypesToLock = _appMetadata.DataTypes.FindAll(dt => dt.TaskId == taskId);

            foreach (DataType dataType in dataTypesToLock)
            {
                bool generatePdf = dataType.AppLogic != null;

                foreach (DataElement dataElement in instance.Data.FindAll(de => de.DataType == dataType.Id))
                {
                    dataElement.Locked = true;
                    _logger.LogInformation($"Locking data element {dataElement.Id} of dataType {dataType.Id}.");
                    Task updateData = _dataService.Update(instance, dataElement);

                    if (generatePdf)
                    {
                        Type dataElementType = GetAppModelType(dataType.AppLogic.ClassRef);
                        Task createPdf = GenerateAndStoreReceiptPDF(instance, taskId, dataElement, dataElementType);
                        await Task.WhenAll(updateData, createPdf);
                    }
                    else
                    {
                        await updateData;
                    }
                }
            }

            if (_appMetadata.AutoDeleteOnProcessEnd)
            {
                int instanceOwnerPartyId = int.Parse(instance.InstanceOwner.PartyId);
                Guid instanceGuid = Guid.Parse(instance.Id.Split("/")[1]);

                await _instanceService.DeleteInstance(instanceOwnerPartyId, instanceGuid, true);
            }

            SendViaeFormidling(instance, taskId);

            await Task.CompletedTask;
        }

        private void VerifyEndOfProcess()
        {
            //ToDo:
        }

        //public virtual async Task<string> GetEFormidlingReceiver(Instance instance)
        //{
        //    // could we get the receiver from appmetadata here? 
        //    return await Task.FromResult(string.Empty);
        //}

        private async void RetrieveSchemaData(Instance instance, string taskId)
        {
            Guid instanceGuid = Guid.Parse(instance.Id.Split("/")[1]);
            int instanceOwnerPartyId = int.Parse(instance.InstanceOwner.PartyId);

            // FindAll de => de.DataType == dataType.Id))

            foreach (DataType dataType in _appMetadata.DataTypes.Where(dt => dt.TaskId == taskId && dt.AppLogic?.AutoCreate == true))
            {
                foreach (DataElement dataElement in instance.Data)
                {
                    Type dataElementType = GetAppModelType(dataType.AppLogic.ClassRef);
                    object data = await _dataService.GetFormData(instanceGuid, dataElementType, instance.Org, instance.AppId, instanceOwnerPartyId, new Guid(dataElement.Id));
                }
            }
        }

        private async void RetrieveAndSendBinaryAttachments(Instance instance)
        {
            Guid instanceGuid = Guid.Parse(instance.Id.Split("/")[1]);
            int instanceOwnerPartyId = int.Parse(instance.InstanceOwner.PartyId);

            List<Stream> attachments = new List<Stream>();
            List<(string, Stream)> list = new List<(string, Stream)>();
            
            //var attachmentList = _dataService.GetBinaryDataList(instance.Org, instance.AppId, instanceOwnerPartyId, instanceGuid).Result;

            foreach (DataElement dataElement in instance.Data)
            {
                if (dataElement.Filename != null)
                {
                    using (Stream fileStream = _dataService.GetBinaryData(instance.Org, instance.AppId, instanceOwnerPartyId, instanceGuid, new Guid(dataElement.Id)).Result)
                    {
                        var sendBinary = await _eFormidlingClient.UploadAttachment(fileStream, instanceGuid.ToString(), dataElement.Filename);
                    }
                   
                    // attachments.Add(fileStream);
                    //list.Add((dataElement.Filename, fileStream));
                    //SaveStreamAsFile(@"C:\Users\Trond\Altinn\Altinn.EFormidlingClient\XUnitTestFormidling\TestData\output", fileStream, dataElement.Filename);
                }
            }   
        }

        private bool VerifyCapabilities(Capabilities capabilities)
        {
            // By App developer
            // "urn:no:difi:profile:arkivmelding:planByggOgGeodata:ver1.0";
            string type = "arkivmelding";
            string serviceIdentifier = "DPO";
            bool isValid = false;

            foreach (var capability in capabilities.Capability)
            {
                if (capability.ServiceIdentifier == serviceIdentifier)
                {
                    foreach (var docType in capability.DocumentTypes)
                    {
                        if (docType.Type == type)
                        {
                            isValid = true;
                        }
                    }
                }
            }

            return isValid;
        }

        private StandardBusinessDocument ConstructSBD(Instance instance)
        {
            // Prefilled - replace config
            JObject sbdJson = JObject.Parse(File.ReadAllText(@"C:\Users\Trond\Altinn\Altinn.EFormidlingClient\XUnitTestFormidling\TestData\sbd.json"));
            StandardBusinessDocument sbd = JsonConvert.DeserializeObject<StandardBusinessDocument>(sbdJson.ToString());

            string type = "arkivmelding";

            string process = "urn:no:difi:profile:arkivmelding:administrasjon:ver1.0";
            
            //EFormidlingContract sbdConfig = new EFormidlingContract();
            //sbdConfig.ServiceId = "DPO";
            //sbdConfig.Receiver = "910075918";
            //sbdConfig.Process = process;
            //sbdConfig.DataTypes = type;
                
            DateTime currentCreationTime = DateTime.Now;
            DateTime currentCreationTime2HoursLater = currentCreationTime.AddHours(2);
            string instanceGuid = Guid.Parse(instance.Id.Split("/")[1]).ToString();

            sbd.StandardBusinessDocumentHeader.BusinessScope.Scope.First().Identifier = process;
            sbd.StandardBusinessDocumentHeader.BusinessScope.Scope.First().InstanceIdentifier = instanceGuid;
            sbd.StandardBusinessDocumentHeader.BusinessScope.Scope.First().ScopeInformation.First().ExpectedResponseDateTime = currentCreationTime2HoursLater;
            sbd.StandardBusinessDocumentHeader.DocumentIdentification.Type = type;
            sbd.StandardBusinessDocumentHeader.DocumentIdentification.InstanceIdentifier = instanceGuid;
            sbd.StandardBusinessDocumentHeader.DocumentIdentification.CreationDateAndTime = currentCreationTime;

            return sbd;
        }

        private static void SaveStreamAsFile(string filePath, Stream inputStream, string fileName)
        {
            DirectoryInfo info = new DirectoryInfo(filePath);
            if (!info.Exists)
            {
                info.Create();
            }

            string path = Path.Combine(filePath, fileName);
            using (FileStream outputFileStream = new FileStream(path, FileMode.Create))
            {
                inputStream.CopyTo(outputFileStream);
            }
        }

        private async void SendViaeFormidling(Instance instance, string taskId)
        {
            var guid = instance.Id;

            // Guid instanceGuid = Guid.Parse(instance.Id.Split("/")[1]);
            int instanceOwnerPartyId = int.Parse(instance.InstanceOwner.PartyId);
            string instanceGuid = Guid.Parse(instance.Id.Split("/")[1]).ToString();

            //If metadataConfig SendViaeFormindg == True:

           // var attachments = RetrieveBinaryAttachments(instance);

            //We Won't use capabilities

            //Capabilities capabilities = await _eFormidlingClient.GetCapabilities("910075918");
            //bool isValid = VerifyCapabilities(capabilities);
       
            var sbd = ConstructSBD(instance);
            StandardBusinessDocument sbdVerified = await _eFormidlingClient.CreateMessage(sbd);
            RetrieveAndSendBinaryAttachments(instance);

            //foreach (var attachment in attachments)
            //{
            //    var fileName = attachment.Item1;
            //    var stream = attachment.Item2;
            //    var sendBinary = await _eFormidlingClient.UploadAttachment(stream, instanceGuid, fileName);
            //}

            string filename = "arkivmelding.xml";
            using (FileStream fs2 = File.OpenRead(@"C:\Users\Trond\Altinn\Altinn.EFormidlingClient\XUnitTestFormidling\TestData\arkivmelding.xml"))
            {
                if (fs2.Length > 3)
                {
                    var sendArkivmelding = await _eFormidlingClient.UploadAttachment(fs2, instanceGuid, filename);
                }
            }

            var completeSending = await _eFormidlingClient.SendMessage(instanceGuid);
            Statuses thisMessageStatusAfter = await _eFormidlingClient.GetMessageStatusById(instanceGuid);               
        }

        private async Task GenerateAndStoreReceiptPDF(Instance instance, string taskId, DataElement dataElement, Type dataElementModelType)
        {
            string app = instance.AppId.Split("/")[1];
            string org = instance.Org;
            int instanceOwnerId = int.Parse(instance.InstanceOwner.PartyId);
            Guid instanceGuid = Guid.Parse(instance.Id.Split("/")[1]);

            string layoutSetsString = _resourceService.GetLayoutSets();
            LayoutSets layoutSets = null;
            LayoutSet layoutSet = null;
            if (!string.IsNullOrEmpty(layoutSetsString))
            {
                layoutSets = JsonConvert.DeserializeObject<LayoutSets>(layoutSetsString);
                layoutSet = layoutSets.Sets.FirstOrDefault(t => t.DataType.Equals(dataElement.DataType) && t.Tasks.Contains(taskId));
            }

            string layoutSettingsFileContent = layoutSet == null ? _resourceService.GetLayoutSettings() : _resourceService.GetLayoutSettingsForSet(layoutSet.Id);

            LayoutSettings layoutSettings = null;
            if (!string.IsNullOrEmpty(layoutSettingsFileContent))
            {
                layoutSettings = JsonConvert.DeserializeObject<LayoutSettings>(layoutSettingsFileContent);
            }

            object data = await _dataService.GetFormData(instanceGuid, dataElementModelType, org, app, instanceOwnerId, new Guid(dataElement.Id));

            layoutSettings = await FormatPdf(layoutSettings, data);
            XmlSerializer serializer = new XmlSerializer(dataElementModelType);
            using MemoryStream stream = new MemoryStream();

            serializer.Serialize(stream, data);
            stream.Position = 0;

            byte[] dataAsBytes = new byte[stream.Length];
            await stream.ReadAsync(dataAsBytes);
            string encodedXml = Convert.ToBase64String(dataAsBytes);

            UserContext userContext = await _userHelper.GetUserContext(_httpContextAccessor.HttpContext);
            UserProfile userProfile = await _profileService.GetUserProfile(userContext.UserId);

            // If layoutst exist pick correctr layotFiles
            string formLayoutsFileContent = layoutSet == null ? _resourceService.GetLayouts() : _resourceService.GetLayoutsForSet(layoutSet.Id);

            TextResource textResource = await _textService.GetText(org, app, userProfile.ProfileSettingPreference.Language);
            if (textResource == null && !userProfile.ProfileSettingPreference.Equals("nb"))
            {
                // fallback to norwegian if texts does not exist
                textResource = await _textService.GetText(org, app, "nb");
            }

            string textResourcesString = JsonConvert.SerializeObject(textResource);
            Dictionary<string, Dictionary<string, string>> optionsDictionary = await GetOptionsDictionary(formLayoutsFileContent);

            PDFContext pdfContext = new PDFContext
            {
                Data = encodedXml,
                FormLayouts = JsonConvert.DeserializeObject<Dictionary<string, object>>(formLayoutsFileContent),
                LayoutSettings = layoutSettings,
                TextResources = JsonConvert.DeserializeObject(textResourcesString),
                OptionsDictionary = optionsDictionary,
                Party = await _registerService.GetParty(instanceOwnerId),
                Instance = instance,
                UserProfile = userProfile,
                UserParty = userProfile.Party
            };

            Stream pdfContent = await _pdfService.GeneratePDF(pdfContext);
            await StorePDF(pdfContent, instance, textResource);
            pdfContent.Dispose();
        }

        private async Task<DataElement> StorePDF(Stream pdfStream, Instance instance, TextResource textResource)
        {
            string fileName = null;
            string app = instance.AppId.Split("/")[1];

            TextResourceElement titleText = textResource.Resources.Find(textResourceElement => textResourceElement.Id.Equals("ServiceName"));

            if (titleText != null && !string.IsNullOrEmpty(titleText.Value))
            {
                fileName = titleText.Value + ".pdf";
            }
            else
            {
                fileName = app + ".pdf";
            }

            fileName = GetValidFileName(fileName);

            return await _dataService.InsertBinaryData(
                instance.Id,
                pdfElementType,
                "application/pdf",
                fileName,
                pdfStream);
        }

        private string GetValidFileName(string fileName)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }

            fileName = Uri.EscapeDataString(fileName);
            return fileName;
        }

        private List<string> GetOptionIdsFromFormLayout(string formLayout)
        {
            List<string> optionsIds = new List<string>();
            string matchString = "\"optionsId\":\"";

            string[] formLayoutSubstrings = formLayout.Replace(" ", string.Empty).Split(new string[] { matchString }, StringSplitOptions.None);

            for (int i = 1; i < formLayoutSubstrings.Length; i++)
            {
                string[] workingSet = formLayoutSubstrings[i].Split('\"');
                string optionsId = workingSet[0];
                optionsIds.Add(optionsId);
            }

            return optionsIds;
        }

        private async Task<Dictionary<string, Dictionary<string, string>>> GetOptionsDictionary(string formLayout)
        {
            Dictionary<string, Dictionary<string, string>> dictionary = new Dictionary<string, Dictionary<string, string>>();
            List<string> optionsIdsList = GetOptionIdsFromFormLayout(formLayout);

            foreach (string optionsId in optionsIdsList)
            {
                AppOptions appOptions = new AppOptions();

                appOptions.Options = _resourceService.GetOptions(optionsId);
                appOptions = await GetOptions(optionsId, appOptions);

                if (appOptions.Options != null && !dictionary.ContainsKey(optionsId))
                {
                    Dictionary<string, string> options = new Dictionary<string, string>();
                    foreach (AppOption item in appOptions.Options)
                    {
                        options.Add(item.Label, item.Value);
                    }

                    dictionary.Add(optionsId, options);
                }          
            }

            return dictionary;
        }
    }
}
