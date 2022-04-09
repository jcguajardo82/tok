using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

using Soriana.PPS.Common.Constants;
using Soriana.PPS.Common.DTO.Card;
using Soriana.PPS.DataAccess.Card;
using System;
using Soriana.PPS.Common.DTO.ClosureOrder;

namespace Soriana.PPS.Card.SorianaApp.Services
{
    public class SorianaAppService : ISorianaAppService
    {
        #region Private Fields
        private readonly ILogger<SorianaAppService> _Logger;
        private readonly ICardContext _CardContext;
        #endregion

        #region Constructor
        public SorianaAppService(ILogger<SorianaAppService> logger, ICardContext cardContext)
        {
            _Logger = logger;
            _CardContext = cardContext;
        }
        #endregion

        #region Public Methods
        public async Task<AddCardResponse> AddCard(AddCardSorianaAppRequest entity, string SalesForceRequest)
        {
            #region Definiciones
            string paymentCardId = string.Empty;
            AddCardResponse CardResponse = new AddCardResponse();
            CardTokenRequest Card = new CardTokenRequest();
            string OmonelToken = string.Empty;
            string BlobService = Environment.GetEnvironmentVariable("BlobAPIService");
            #endregion

            #region Save Json Request
            //string strSalesForceRequest = SalesForceRequest.Replace("\"", "");
            //await _CardContext.SaveRequestSalesForce(entity.customerId, strSalesForceRequest);
            #endregion

            var BinCard = await _CardContext.GetBinCard(entity.binCard);

 
            #region PaymentBlob
            BlobServiceRequest blobServiceResponse = new BlobServiceRequest
            {
                merchantID = "soriana_mx",
                MerchantReferenceCode = entity.merchantReferenceCode,
                encryptedPayment_data = entity.encryptedPayment_data,
                paymentSolution = "004",
                billTo_firstName = entity.billToFirstName,
                billTo_lastName = entity.billToLastName,
                billTo_city = entity.billToCity,
                billTo_country = "MX",
                billTo_email = entity.billToEmail,
                billTo_state = entity.billToState,
                billTo_street1 = entity.billToStreet1,
                billTo_street2 = entity.billToStreet2,
                billTo_postalCode = entity.billToPostalCode,
                purchaseTotals_currency = "MXN",
                purchaseTotals_grandTotalAmount = "1",
                recurringSubscriptionInfo_frequency = "on-demand",
            };

            #region Trace Payment
            string Request = JsonConvert.SerializeObject(blobServiceResponse);
            TracePayment tracePaymentReqBlob = new TracePayment
            {
                method = "AddCardAPP_BlobService_INI",
                order = entity.customerId,
                request = Request,
                response = ""
            };

            await SaveLogTracePayment(tracePaymentReqBlob);
            #endregion

            #region POST
            string BlobJsonRequest = JsonConvert.SerializeObject(blobServiceResponse);

            FmkTools.RestResponse responseApi = FmkTools.RestClient.RequestRest_1(FmkTools.HttpVerb.POST, BlobService, null, BlobJsonRequest);
            string jsonResponse = responseApi.message;

            #region Trace Payment
            TracePayment tracePaymentResponseBlob = new TracePayment
            {
                method = "AddCardAPP_BlobService_FIN",
                order = entity.customerId,
                request = Request,
                response = jsonResponse
            };

            await SaveLogTracePayment(tracePaymentResponseBlob);
            #endregion

            BlobServiceResponse blobResponse = JsonConvert.DeserializeObject<BlobServiceResponse>(jsonResponse);
            #endregion

            #endregion

            if(blobResponse.responseMessage != "RECHAZADA")
            {
                #region BinCode
                if (BinCard.Count != 0)
                {
                    if (BinCard[0].Id_Cve_Bin == ConfigurationConstants.OMONEL_BIN_CODE)
                    {
                        var guid = Guid.NewGuid();
                        Card.CardAccountNumber = entity.cardAccountNumber;
                        Card.NipRequired = true;
                        Card.CvvRequired = false;
                        Card.ClientToken = "OMONEL-" + guid.ToString();
                        OmonelToken = "OMONEL-" + guid.ToString();
                    }
                    else
                    {
                        #region PaymentCard                 
                        paymentCardId = blobResponse.JsonResponse.paySubscriptionCreateReply_instrumentIdentifierID + "-" + blobResponse.JsonResponse.paySubscriptionCreateReply_subscriptionID;
                        #endregion

                        Card.NipRequired = false;
                        Card.CvvRequired = true;
                        Card.ClientToken = paymentCardId;
                    }

                    Card.Bank = BinCard[0].Nom_EmisorTjta;
                    Card.BinCode = int.Parse(BinCard[0].Id_Cve_Bin);
                    Card.ClientID = entity.customerId;
                    Card.CustomerID = entity.customerId;
                    Card.paymentCardId = paymentCardId;
                    Card.PaymentType = BinCard[0].Desc_Marca;
                    Card.TypeOfCard = BinCard[0].Desc_TipoTjta;
                    Card.PersistToken = bool.Parse(entity.setAsdefault);
                    Card.IsActive = true;
                    Card.MaskCard = entity.binCard.Substring(0, 4) + "-XXXX-XXXX-" + entity.suffix;
                    Card.IsDefault = bool.Parse(entity.setAsdefault);

                    if (blobResponse.JsonResponse.decision != "REJECT")
                    {
                        //TODO: AGREGAR DATOS DE LA RESPUESTA DE CYBER
                        CardResponse = await CardsToken(Card, entity, OmonelToken);

                        if (CardResponse.responseError == "0")
                        {
                            await CustomerInfo(entity, Card);
                        }
                    }
                    else
                    {
                        CardResponse.responseError = "06";
                        CardResponse.responseMessage = "Error al tokenizar la tarjeta.";
                        CardResponse.paymentCardMask = "";
                        CardResponse.paymentCardName = "";
                        CardResponse.paymentCardToken = "";
                        CardResponse.paymentCardBank = "";
                        CardResponse.paymentCardIcon = "";
                    }

                }
                else
                {
                    CardResponse.responseError = "12";
                    CardResponse.responseMessage = "BinCode no se encuentra registrado";
                    CardResponse.paymentCardMask = "";
                    CardResponse.paymentCardName = "";
                    CardResponse.paymentCardToken = "";
                    CardResponse.paymentCardBank = "";
                    CardResponse.paymentCardIcon = "";

                    return CardResponse;
                }
                #endregion
            }
            else
            {
                CardResponse.responseError = "13";
                CardResponse.responseMessage = "Error al tokenizar la tarjeta. " + blobResponse.responseError;
                CardResponse.paymentCardMask = "";
                CardResponse.paymentCardName = "";
                CardResponse.paymentCardToken = "";
                CardResponse.paymentCardBank = "";
                CardResponse.paymentCardIcon = "";
            }

            return CardResponse;
        }

        public async Task<AddCardResponse> UpdatePredeterminatedCard(AddCardSorianaAppRequest entity)
        {
            AddCardResponse CardResponse = new AddCardResponse();

            if (entity.binCard == ConfigurationConstants.OMONEL_BIN_CODE)
            {
                var ValidateOmonelCard = await _CardContext.GetClientDetailOmonel_APP(entity.customerId, entity.paymentCardId);
                var DetailOmonelCard = await _CardContext.GetClientDetailOmonel(entity.customerId, ValidateOmonelCard[0].CardAccountNumber);

                if (ValidateOmonelCard.Count == 0)
                {
                    CardResponse.responseError = "09";
                    CardResponse.responseMessage = "Tarjeta Omonel no se encuentra previamente registrada";
                    CardResponse.paymentCardMask = "";
                    CardResponse.paymentCardName = "";
                    CardResponse.paymentCardToken = "";
                    CardResponse.paymentCardBank = "";
                    CardResponse.paymentCardIcon = "";
                }
                else
                {
                    await _CardContext.UpdatePredeterminatedCardOmonel_APP(entity.paymentCardId, entity.customerId);

                    CardResponse.responseError = "00";
                    CardResponse.responseMessage = "";
                    CardResponse.paymentCardMask = ValidateOmonelCard[0].MaskCard;
                    CardResponse.paymentCardName = DetailOmonelCard[0].billToFirstName + " " + DetailOmonelCard[0].billToLastName;
                    CardResponse.paymentCardToken = ValidateOmonelCard[0].ClientToken;
                    CardResponse.paymentCardBank = ValidateOmonelCard[0].Bank;
                    CardResponse.paymentCardIcon = "";
                }
            }
            else
            {
                var ValidateTokenCard = await _CardContext.GetClientDetail(entity.customerId, entity.paymentCardId);

                if (ValidateTokenCard.Count == 0)
                {
                    CardResponse.responseError = "09";
                    CardResponse.responseMessage = "Tarjeta no se encuentra previamente registrada";
                    CardResponse.paymentCardMask = "";
                    CardResponse.paymentCardName = "";
                    CardResponse.paymentCardToken = "";
                    CardResponse.paymentCardBank = "";
                    CardResponse.paymentCardIcon = "";
                }
                else
                {
                    await _CardContext.UpdatePredeterminatedCard(entity.paymentCardId, entity.customerId);

                    CardResponse.responseError = "00";
                    CardResponse.responseMessage = "";
                    CardResponse.paymentCardMask = ValidateTokenCard[0].MaskCard;
                    CardResponse.paymentCardName = ValidateTokenCard[0].billToFirstName + " " + ValidateTokenCard[0].billToLastName; ;
                    CardResponse.paymentCardToken = ValidateTokenCard[0].ClientToken;
                    CardResponse.paymentCardBank = ValidateTokenCard[0].Bank;
                    CardResponse.paymentCardIcon = "";
                }
            }

            return CardResponse;
        }
        #endregion

        #region Private Methods
        public async Task<AddCardResponse> CardsToken(CardTokenRequest Card, AddCardSorianaAppRequest entity, string OmonelToken)
        {
        AddCardResponse CardResponse = new AddCardResponse();
        IList<ValidateTokenResponse> ValidateTokenCard = null;

        if (Card.BinCode == int.Parse(ConfigurationConstants.OMONEL_BIN_CODE))
        {
            ValidateTokenCard = await _CardContext.ValidateOmonelCard(Card);
        }
        else
        {
            ValidateTokenCard = await _CardContext.ValidateTokenCard(Card);
        }

        if (ValidateTokenCard.Count == 0)
        {
            if (Card.BinCode == int.Parse(ConfigurationConstants.OMONEL_BIN_CODE))
                await _CardContext.SaveCardOmonel(Card);
            else
                await _CardContext.SaveCard(Card);

            CardResponse.responseError = "0";
            CardResponse.responseMessage = "";
            CardResponse.paymentCardMask = entity.binCard.Substring(0, 4) + "-XXXX-XXXX-" + entity.suffix;
            CardResponse.paymentCardName = entity.billToFirstName + " " + entity.billToLastName;
            CardResponse.paymentCardToken = Card.ClientToken;
            CardResponse.paymentCardIcon = "";

            if (Card.BinCode == int.Parse(ConfigurationConstants.OMONEL_BIN_CODE))
            {
                CardResponse.paymentCardBank = "OMONEL";
                CardResponse.paymentCardToken = OmonelToken;
            }
            else
            {
                CardResponse.paymentCardBank = Card.Bank;
            }
        }
        else if (ValidateTokenCard[0].IsActive == false)
        {
            if (Card.BinCode == int.Parse(ConfigurationConstants.OMONEL_BIN_CODE))
                await _CardContext.UpdateCardOmonel(entity.customerId, entity.cardAccountNumber);
            else
                await _CardContext.UpdateCard(entity.customerId, entity.paymentCardId);

            CardResponse.responseError = "0";
            CardResponse.responseMessage = "";
            CardResponse.paymentCardMask = entity.binCard.Substring(0, 4) + "-XXXX-XXXX-" + entity.suffix;
            CardResponse.paymentCardName = entity.billToFirstName + " " + entity.billToLastName; ;
            CardResponse.paymentCardIcon = "";

            if (Card.BinCode == int.Parse(ConfigurationConstants.OMONEL_BIN_CODE))
            {
                CardResponse.paymentCardBank = "OMONEL";
                CardResponse.paymentCardToken = OmonelToken;
            }
            else
            {
                CardResponse.paymentCardBank = Card.Bank;
            }
        }
        else
        {
            CardResponse.responseError = "06";
            CardResponse.responseMessage = "Tarjeta ya se encuentra previamente registrada";
            CardResponse.paymentCardMask = "";
            CardResponse.paymentCardName = "";
            CardResponse.paymentCardToken = "";
            CardResponse.paymentCardBank = "";
            CardResponse.paymentCardIcon = "";
        }

        return CardResponse;
        }

        public async Task CustomerInfo(AddCardSorianaAppRequest entity, CardTokenRequest card)
        {
            ClientCard client = new ClientCard
            {
                ClientID = entity.customerId,
                billToEmail = entity.billToEmail,
                billToFirstName = entity.billToFirstName,
                billToLastName = entity.billToLastName,
                billToCity = entity.billToCity,
                billToCountry = entity.billToCountry,
                billToPhoneNumber = entity.billToPhoneNumber,
                billToPostalCode = entity.billToPostalCode,
                billToState = entity.billToState,
                billToStreet1 = entity.billToStreet1
            };

            if (entity.binCard == ConfigurationConstants.OMONEL_BIN_CODE)
                client.ClientToken = card.CardAccountNumber;
            else
                client.ClientToken = card.paymentCardId;

            await _CardContext.SaveCustomerInfo(client);
        }

        private async Task SaveLogTracePayment(TracePayment tracePayment)
        {
            await _CardContext.SaveLogTracePayment(tracePayment);
        }
        #endregion
    }
}
