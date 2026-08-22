import { useState } from 'react';
import { PayPalScriptProvider, PayPalButtons } from '@paypal/react-paypal-js';
import { createPayPalOrder, capturePayPalOrder } from '../../api/orderApi';
import { useToast } from '../Toast';
import { formatCurrency } from '../../utils/productUtils';
import './PayPalButtonsSection.css';

const PayPalButtonsSection = ({ order, onPaymentSuccess }) => {
  const { showSuccess, showError } = useToast();

  if (!order || order.status !== 'PENDING_PAYMENT') return null;

  const method = order.paymentMethod?.toUpperCase();
  if (method !== 'PAYPAL') return null;

  const amountToPay = order.latestPayment?.amount || order.totalPrice;

  const initialOptions = {
    // using VITE_ prefix for Vite environment variables
    "client-id": import.meta.env.VITE_PAYPAL_CLIENT_ID || "test",
    currency: "USD",
    intent: "capture",
  };

  const createOrder = async () => {
    try {
      const data = await createPayPalOrder(order.id);
      if (!data || !data.paypalOrderId) {
        throw new Error('No valid PayPal Order ID received from server.');
      }
      return data.paypalOrderId;
    } catch (error) {
      showError(error.message || 'Failed to initialize PayPal order.');
      // Returning null implicitly cancels the PayPal button flow
    }
  };

  const onApprove = async (data, actions) => {
    try {
      await capturePayPalOrder(order.id, data.orderID);
      showSuccess('Payment captured successfully!');
      if (onPaymentSuccess) {
        onPaymentSuccess();
      }
    } catch (error) {
      showError(error.message || 'Failed to capture PayPal payment.');
    }
  };

  const onError = (err) => {
    console.error('PayPal Checkout Error:', err);
    showError('PayPal checkout encountered an error. Please try again.');
  };

  return (
    <div className="paypal-payment-section">
      <div className="paypal-payment-info">
        <h4 className="paypal-title">Complete your payment securely</h4>
        <div className="paypal-details">
          <span>Amount due: <strong>{formatCurrency(amountToPay)}</strong></span>
          <span className="payment-method-badge">PayPal</span>
        </div>
      </div>
      <div className="paypal-buttons-wrapper">
        <PayPalScriptProvider options={initialOptions}>
          <PayPalButtons
            style={{ layout: "vertical", color: "gold", shape: "rect", label: "pay" }}
            createOrder={createOrder}
            onApprove={onApprove}
            onError={onError}
          />
        </PayPalScriptProvider>
      </div>
    </div>
  );
};

export default PayPalButtonsSection;
