﻿using System.Linq;
using Merchello.Core;

namespace Merchello.Plugin.Payments.SagePay.Provider
{
    using Merchello.Core.Gateways;
    using Merchello.Core.Gateways.Payment;
    using Merchello.Core.Models;
    using Merchello.Core.Services;

    /// <summary>
    /// Represents a SagePayGatewayMethod for Merchello.
    /// </summary>
    [GatewayMethodUi("SagePayIFrame")]
    [PaymentGatewayMethod("SagePay IFrame Method Editors",
        "~/App_Plugins/Merchello.SagePay/",
        "~/App_Plugins/Merchello.SagePay/",
        "~/App_Plugins/Merchello.SagePay/")]
    public class SagePayPaymentGatewayMethod : PaymentGatewayMethodBase, ISagePayPaymentGatewayMethod       
    {
        private readonly SagePayPaymentProcessor _processor;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SagePayPaymentGatewayMethod"/> class.
        /// </summary>
        /// <param name="gatewayProviderService">
        /// The <see cref="GatewayProviderService"/>.
        /// </param>
        /// <param name="paymentMethod">
        /// The <see cref="IPaymentMethod"/>.
        /// </param>
        /// <param name="extendedData">
        /// The SagePay providers <see cref="ExtendedDataCollection"/>
        /// </param>
        public SagePayPaymentGatewayMethod(IGatewayProviderService gatewayProviderService, IPaymentMethod paymentMethod, ExtendedDataCollection extendedData)
            : base(gatewayProviderService, paymentMethod)
        {
            // New instance of the SagePay payment processor
            _processor = new SagePayPaymentProcessor(extendedData.GetProcessorSettings());
        }

        /// <summary>
        /// Does the actual work of creating and processing the payment
        /// </summary>
        /// <param name="invoice">The <see cref="IInvoice"/></param>
        /// <param name="args">Any arguments required to process the payment.</param>
        /// <returns>The <see cref="IPaymentResult"/></returns>
        protected override IPaymentResult PerformAuthorizePayment(IInvoice invoice, ProcessorArgumentCollection args)
        {
            return InitializePayment(invoice, args, -1);
        }


        private IPaymentResult InitializePayment(IInvoice invoice, ProcessorArgumentCollection args, decimal captureAmount)
        {
            var payment = GatewayProviderService.CreatePayment(PaymentMethodType.CreditCard, invoice.Total, PaymentMethod.Key);
            payment.CustomerKey = invoice.CustomerKey;
            payment.Authorized = false;
            payment.Collected = false;
            payment.PaymentMethodName = "SagePay";
            payment.ExtendedData.SetValue(Constants.ExtendedDataKeys.CaptureAmount, captureAmount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            GatewayProviderService.Save(payment);

            var result = _processor.InitializePayment(invoice, payment, args);

            if (!result.Payment.Success)
            {
                GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Denied, "SagePay: request initialization error: " + result.Payment.Exception.Message, 0);
            }
            else
            {
                GatewayProviderService.Save(payment);
                GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Debit, "SagePay: initialized", 0);
            }

            return result;
        }

        /// <summary>
        /// Does the actual work capturing a payment
        /// </summary>
        /// <param name="invoice">The <see cref="IInvoice"/></param>
        /// <param name="payment">The previously Authorize payment to be captured</param>
        /// <param name="amount">The amount to capture</param>
        /// <param name="args">Any arguments required to process the payment.</param>
        /// <returns>The <see cref="IPaymentResult"/></returns>
        protected override IPaymentResult PerformCapturePayment(IInvoice invoice, IPayment payment, decimal amount, ProcessorArgumentCollection args)
        {
            var payedTotalList = invoice.AppliedPayments().Select(item => item.Amount).ToList();
            var payedTotal = (payedTotalList.Count == 0 ? 0 : payedTotalList.Aggregate((a, b) => a + b));
            var isPartialPayment = amount + payedTotal < invoice.Total;

            var result = _processor.CapturePayment(invoice, payment, amount, isPartialPayment);
            //GatewayProviderService.Save(payment);

            if (!result.Payment.Success)
            {
                //payment.VoidPayment(invoice, payment.PaymentMethodKey.Value);
                GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Denied, "PayPal: request capture error: " + result.Payment.Exception.Message, 0);
            }
            else
            {
                GatewayProviderService.Save(payment);
                GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Debit, "PayPal: captured", amount);
                //GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Debit, payment.ExtendedData.GetValue(Constants.ExtendedDataKeys.CaptureTransactionResult), amount);
            }


            return result;
        }


        /// <summary>
        /// Does the actual work of authorizing and capturing a payment
        /// </summary>
        /// <param name="invoice">The <see cref="IInvoice"/></param>
        /// <param name="amount">The amount to capture</param>
        /// <param name="args">Any arguments required to process the payment.</param>
        /// <returns>The <see cref="IPaymentResult"/></returns>
        protected override IPaymentResult PerformAuthorizeCapturePayment(IInvoice invoice, decimal amount, ProcessorArgumentCollection args)
        {
            // SERVER Side implementation ... probably not need for the IFRAME method
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Does the actual work of refunding a payment
        /// </summary>
        /// <param name="invoice">The <see cref="IInvoice"/></param>
        /// <param name="payment">The previously Authorize payment to be captured</param>
        /// <param name="amount">The amount to be refunded</param>
        /// <param name="args">Any arguments required to process the payment.</param>
        /// <returns>The <see cref="IPaymentResult"/></returns>
        protected override IPaymentResult PerformRefundPayment(IInvoice invoice, IPayment payment, decimal amount, ProcessorArgumentCollection args)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Does the actual work of voiding a payment
        /// </summary>
        /// <param name="invoice">The invoice to which the payment is associated</param>
        /// <param name="payment">The payment to be voided</param>
        /// <param name="args">Additional arguments required by the payment processor</param>
        /// <returns>A <see cref="IPaymentResult"/></returns>
        protected override IPaymentResult PerformVoidPayment(IInvoice invoice, IPayment payment, ProcessorArgumentCollection args)
        {
            throw new System.NotImplementedException();
        }
    }
}