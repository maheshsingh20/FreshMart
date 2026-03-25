import { Injectable, inject } from '@angular/core';
import { AuthService } from './auth.service';
import { Order } from '../models';

@Injectable({ providedIn: 'root' })
export class InvoiceService {
  private auth = inject(AuthService);

  generate(order: Order): void {
    const userName = this.auth.getUserName() ?? 'Customer';
    const orderRef = order.id.slice(0, 8).toUpperCase();
    const date = new Date(order.createdAt).toLocaleDateString('en-IN', { day: '2-digit', month: 'long', year: 'numeric' });
    const deliveredDate = order.deliveredAt
      ? new Date(order.deliveredAt).toLocaleDateString('en-IN', { day: '2-digit', month: 'long', year: 'numeric' })
      : '—';

    const itemRows = order.items.map(i => `
      <tr>
        <td style="padding:10px 12px;border-bottom:1px solid #f3f4f6;">${i.productName}</td>
        <td style="padding:10px 12px;border-bottom:1px solid #f3f4f6;text-align:center;">${i.quantity}</td>
        <td style="padding:10px 12px;border-bottom:1px solid #f3f4f6;text-align:right;">₹${i.unitPrice.toFixed(2)}</td>
        <td style="padding:10px 12px;border-bottom:1px solid #f3f4f6;text-align:right;font-weight:600;">₹${i.totalPrice.toFixed(2)}</td>
      </tr>`).join('');

    const discountRow = order.discountAmount > 0 ? `
      <tr>
        <td colspan="3" style="padding:6px 12px;text-align:right;color:#16a34a;">Discount</td>
        <td style="padding:6px 12px;text-align:right;color:#16a34a;">- ₹${order.discountAmount.toFixed(2)}</td>
      </tr>` : '';

    const html = `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <title>Invoice #${orderRef} — FreshMart</title>
  <style>
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body { font-family: 'Segoe UI', Arial, sans-serif; color: #111827; background: #fff; padding: 40px; font-size: 14px; }
    .header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 36px; }
    .brand { display: flex; align-items: center; gap: 10px; }
    .brand-icon { font-size: 32px; }
    .brand-name { font-size: 22px; font-weight: 800; color: #16a34a; }
    .brand-sub { font-size: 11px; color: #6b7280; margin-top: 2px; }
    .invoice-meta { text-align: right; }
    .invoice-title { font-size: 26px; font-weight: 700; color: #111827; letter-spacing: -0.5px; }
    .invoice-num { font-size: 13px; color: #6b7280; margin-top: 4px; }
    .divider { border: none; border-top: 2px solid #f3f4f6; margin: 24px 0; }
    .info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-bottom: 28px; }
    .info-box h3 { font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.8px; color: #9ca3af; margin-bottom: 8px; }
    .info-box p { font-size: 13px; color: #374151; line-height: 1.6; }
    .info-box .highlight { font-weight: 700; color: #111827; font-size: 14px; }
    table { width: 100%; border-collapse: collapse; margin-bottom: 0; }
    thead tr { background: #f9fafb; }
    thead th { padding: 10px 12px; text-align: left; font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.6px; color: #6b7280; border-bottom: 2px solid #e5e7eb; }
    thead th:not(:first-child) { text-align: center; }
    thead th:last-child { text-align: right; }
    .totals { margin-top: 0; border-top: 2px solid #e5e7eb; }
    .totals tr td { padding: 6px 12px; font-size: 13px; color: #6b7280; }
    .totals tr td:first-child { text-align: right; }
    .totals tr td:last-child { text-align: right; min-width: 100px; }
    .total-row td { font-size: 16px; font-weight: 800; color: #111827; padding: 12px 12px; border-top: 2px solid #e5e7eb; }
    .status-badge { display: inline-block; padding: 3px 10px; border-radius: 20px; font-size: 11px; font-weight: 600; }
    .status-Delivered { background: #dcfce7; color: #16a34a; }
    .status-Processing { background: #dbeafe; color: #1d4ed8; }
    .status-Shipped { background: #e0e7ff; color: #4338ca; }
    .status-Cancelled { background: #fee2e2; color: #dc2626; }
    .status-default { background: #f3f4f6; color: #374151; }
    .footer { margin-top: 40px; padding-top: 20px; border-top: 1px solid #f3f4f6; display: flex; justify-content: space-between; align-items: center; }
    .footer p { font-size: 11px; color: #9ca3af; }
    .thank-you { font-size: 13px; font-weight: 600; color: #16a34a; }
    @media print {
      body { padding: 20px; }
      @page { margin: 15mm; }
    }
  </style>
</head>
<body>
  <div class="header">
    <div class="brand">
      <span class="brand-icon">🛒</span>
      <div>
        <div class="brand-name">FreshMart</div>
        <div class="brand-sub">Fresh Groceries Delivered</div>
      </div>
    </div>
    <div class="invoice-meta">
      <div class="invoice-title">INVOICE</div>
      <div class="invoice-num">#${orderRef}</div>
      <div style="margin-top:6px;">
        <span class="status-badge status-${order.status} ${!['Delivered','Processing','Shipped','Cancelled'].includes(order.status) ? 'status-default' : ''}">
          ${order.status}
        </span>
      </div>
    </div>
  </div>

  <hr class="divider" />

  <div class="info-grid">
    <div class="info-box">
      <h3>Bill To</h3>
      <p class="highlight">${userName}</p>
      <p style="margin-top:4px;">${order.deliveryAddress}</p>
    </div>
    <div class="info-box" style="text-align:right;">
      <h3>Invoice Details</h3>
      <p><strong>Date:</strong> ${date}</p>
      <p><strong>Order ID:</strong> ${orderRef}</p>
      ${order.deliveredAt ? `<p><strong>Delivered:</strong> ${deliveredDate}</p>` : ''}
      ${order.notes ? `<p style="margin-top:6px;font-style:italic;color:#6b7280;">Note: ${order.notes}</p>` : ''}
    </div>
  </div>

  <table>
    <thead>
      <tr>
        <th>Item</th>
        <th style="text-align:center;">Qty</th>
        <th style="text-align:right;">Unit Price</th>
        <th style="text-align:right;">Amount</th>
      </tr>
    </thead>
    <tbody>
      ${itemRows}
    </tbody>
  </table>

  <table class="totals">
    <tbody>
      <tr>
        <td colspan="3" style="text-align:right;color:#6b7280;">Subtotal</td>
        <td style="text-align:right;">₹${order.subTotal.toFixed(2)}</td>
      </tr>
      <tr>
        <td colspan="3" style="text-align:right;color:#6b7280;">Delivery Fee</td>
        <td style="text-align:right;">${order.deliveryFee === 0 ? 'Free' : '₹' + order.deliveryFee.toFixed(2)}</td>
      </tr>
      <tr>
        <td colspan="3" style="text-align:right;color:#6b7280;">Tax (GST)</td>
        <td style="text-align:right;">₹${order.taxAmount.toFixed(2)}</td>
      </tr>
      ${discountRow}
      <tr class="total-row">
        <td colspan="3" style="text-align:right;">Total Amount</td>
        <td style="text-align:right;">₹${order.totalAmount.toFixed(2)}</td>
      </tr>
    </tbody>
  </table>

  <div class="footer">
    <p>FreshMart · support@freshmart.in · freshmart.in</p>
    <p class="thank-you">Thank you for shopping with us! 🙏</p>
  </div>
</body>
</html>`;

    const win = window.open('', '_blank', 'width=800,height=900');
    if (!win) return;
    win.document.write(html);
    win.document.close();
    win.focus();
    setTimeout(() => { win.print(); }, 400);
  }
}
