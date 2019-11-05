using System;
using System.Collections.Generic;
using RSignSDK.Models;
using RSignSDK.Models.MasterData;
using static RSignSDK.Contracts.Enumerations;
using static RSignSDK.RSignAPI;

namespace RSignSDK.Contracts
{
    public interface IRSignAPI : IDisposable
    {
        SendEnvelopeResponse Send(List<DocumentSend> documentSend, string templateName, string recipientEmail, string recipientName, string subject, string body, string expiryType, int reminder1 = 0, int reminder2 = 0, AccessAutenticationTypes accessAutenticationType = AccessAutenticationTypes.Select, string password = "");

        SendEnvelopeResponse Send(string filePath, string documentName, string templateName, string recipientEmail, string recipientName, string subject, string body, string expiryType, int reminder1 = 0, int reminder2 = 0, AccessAutenticationTypes accessAutenticationType = AccessAutenticationTypes.Select, string password = "");

        EnvelopeStatusResponse GetEnvelopeStatus(string envelopeDisplayCode);

        DownloadSignedContractResponse DownloadSignedContract(string envelopeId);

        bool DeleteFinalContract(string envelopeId);

        bool CancelEnvelope(string envelopeId);
    }
}