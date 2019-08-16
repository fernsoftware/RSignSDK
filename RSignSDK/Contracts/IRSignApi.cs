using System;
using System.Collections.Generic;
using RSignSDK.Models;
using RSignSDK.Models.MasterData;

namespace RSignSDK.Contracts
{
    public interface IRSignAPI : IDisposable
    {
        SendEnvelopeResponse Send(List<DocumentSend> documentSend, string templateName, string recipientEmail, string recipientName, string subject, string body, string expiryType, int reminder1 = 0, int reminder2 = 0);

        SendEnvelopeResponse Send(string filePath, string documentName, string templateName, string recipientEmail, string recipientName, string subject, string body, string expiryType, int reminder1 = 0, int reminder2 = 0);

        EnvelopeStatusResponse GetEnvelopeStatus(string envelopeDisplayCode);

        DownloadSignedContractResponse DownloadSignedContract(string envelopeId);

        bool DeleteFinalContract(string envelopeId);

        bool CancelEnvelope(string envelopeId);
    }
}