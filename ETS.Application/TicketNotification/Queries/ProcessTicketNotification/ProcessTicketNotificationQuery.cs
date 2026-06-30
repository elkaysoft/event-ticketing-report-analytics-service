using ETS.Domain.AppConfig;
using ETS.Domain.Common;
using ETS.Domain.Contracts;
using ETS.Domain.Entities;
using ETS.Domain.Enums;
using ETS.Domain.Errors;
using ETS.Domain.Models.Requests;
using ETS.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ETS.Application.TicketNotification.Queries.ProcessTicketNotification
{
    public record ProcessTicketNotificationQuery(Guid OrderId) : IRequest<Result<bool>>;
    

    public class ProcessTicketNotificationQueryHandler(ApplicationDbContext _context,
        IEmailService _emailService,
        IQRCodeService _qRCodeService,
        IDocumentService _documentService,
        IOptions<PostmarkConfigOptions> postmarkOption,
        ILogger<ProcessTicketNotificationQueryHandler> _logger) : 
        IRequestHandler<ProcessTicketNotificationQuery, Result<bool>>
    {        

        public async Task<Result<bool>> Handle(ProcessTicketNotificationQuery request, CancellationToken cancellationToken)
        {
            var orderItem = await _context.OrderItem.Include(i => i.Order)
                    .Include(ev => ev.Order.Event)
                    .FirstOrDefaultAsync(x => x.Id == request.OrderId);
            if(orderItem == null)
            {
                Result.Failure<bool>(OrdersError.NotFound);
            }

            if(orderItem.TicketGenerationStatus == Domain.Enums.TicketGenerationStatus.Completed)
            {
                Result.Failure<bool>(OrdersError.TicketAlreadyGenerated);
            }

            // - Generate QR Code bytes from the unique QR Code reference
            var QRCodeImage = _qRCodeService.GenerateQRCodeBytes(orderItem.QRCodeReference); 

            // - Upload QR code image to cloudinary
            var qrCodeUrl = _documentService.UploadDocument(QRCodeImage);

            var emailData = new TicketEmailData(orderItem.Order.FullName,
                orderItem.Order.EmailAddress,
                orderItem.Order.EventName,
                orderItem.Order.Event.Description,
                orderItem.Order.Event.EventDate,
                orderItem.Order.Event.KickoffTime,
                orderItem.Order.Event.EndTime,
                orderItem.Order.Event.Location,
                orderItem.Title,
                orderItem.Unit,
                orderItem.UnitPrice,
                orderItem.Order.OrderNumber,
                orderItem.QRCodeReference,
                qrCodeUrl);
            var emailBody = BuildHtmlTemplate(emailData);
            string subject = $"Your ticket for {emailData.EventTitle} is confirmed";

            var emailRequest = new SendEmailRequest { HtmlBody = emailBody, Subject = subject, To = emailData.CustomerEmail, From = postmarkOption.Value.SenderEmail };

            if (postmarkOption.Value.Environment.ToUpper() == "TEST")
            {
                emailRequest.To = postmarkOption.Value.TestEmailAddress;
            }

            orderItem.SetQRCodeUrl(qrCodeUrl);
            var emailLog = EmailLog.Create(emailRequest.From, emailData.CustomerEmail, subject, emailBody);
            _context.EmailLogs.Add(emailLog);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("About to process ticket notification for {0}", orderItem.Id);
            
            var emailPushResult = await _emailService.SendEmail(emailRequest, cancellationToken);

            _logger.LogInformation($"Email delivery response for {JsonSerializer.Serialize(emailPushResult)}");

            var notificationStatus = emailPushResult.Message.Equals("OK") ? NotificationStatusEnum.Sent : NotificationStatusEnum.Failed;
            emailLog.UpdateNotificationStatus(JsonSerializer.Serialize(emailPushResult), notificationStatus);
            _context.EmailLogs.Update(emailLog);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }


        private static string BuildHtmlTemplate(TicketEmailData d) => $"""
        <!DOCTYPE html>
        <html>
        <head>
          <meta charset="utf-8"/>
          <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
          <title>Your Ticket — {d.EventTitle}</title>
        </head>
        <body style="margin:0;padding:0;background:#f4f4f4;font-family:Arial,sans-serif;">

          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f4;padding:32px 0;">
            <tr><td align="center">
              <table width="580" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e5e5e5;">

                <!-- Banner -->
                <tr>
                  <td style="background:#185FA5;padding:40px 32px;text-align:center;">
                    <p style="margin:0 0 6px;font-size:11px;letter-spacing:2px;color:#B5D4F4;text-transform:uppercase;">You're going to</p>
                    <h1 style="margin:0;font-size:22px;font-weight:600;color:#E6F1FB;">{d.EventTitle}</h1>
                  </td>
                </tr>

                <!-- Greeting -->
                <tr>
                  <td style="padding:24px 32px 8px;">
                    <p style="margin:0;font-size:15px;color:#555;">Hi {d.CustomerName}, your ticket has been confirmed. Here's everything you need for entry.</p>
                  </td>
                </tr>

                <!-- Section label -->
                <tr>
                  <td style="padding:20px 32px 10px;">
                    <p style="margin:0;font-size:11px;letter-spacing:1.5px;color:#999;text-transform:uppercase;">Ticket summary</p>
                  </td>
                </tr>

                <!-- Ticket stub card -->
                <tr>
                  <td style="padding:0 32px;">
                    <table width="100%" cellpadding="0" cellspacing="0" style="border:1px solid #e5e5e5;border-radius:10px;overflow:hidden;">

                      <!-- Stub header -->
                      <tr>
                        <td style="background:#f9f9f9;padding:14px 16px;border-bottom:1px solid #e5e5e5;">
                          <p style="margin:0;font-size:15px;font-weight:600;color:#111;">{d.EventTitle}</p>
                          <p style="margin:4px 0 0;font-size:12px;color:#777;">{d.EventDescription}</p>
                        </td>
                      </tr>

                      <!-- Stub details grid -->
                      <tr>
                        <td style="padding:16px;">
                          <table width="100%" cellpadding="0" cellspacing="0">
                            <tr>
                              <td width="50%" style="padding-bottom:12px;">
                                <p style="margin:0 0 3px;font-size:11px;color:#999;">📅 Date</p>
                                <p style="margin:0;font-size:13px;font-weight:600;color:#111;">{d.EventDate:ddd, dd MMM yyyy}</p>
                              </td>
                              <td width="50%" style="padding-bottom:12px;">
                                <p style="margin:0 0 3px;font-size:11px;color:#999;">🕓 Time</p>
                                <p style="margin:0;font-size:13px;font-weight:600;color:#111;">{d.StartTime} – {d.EndTime}</p>
                              </td>
                            </tr>
                            <tr>
                              <td width="50%">
                                <p style="margin:0 0 3px;font-size:11px;color:#999;">📍 Venue</p>
                                <p style="margin:0;font-size:13px;font-weight:600;color:#111;">{d.Location}</p>
                              </td>
                              <td width="50%">
                                <p style="margin:0 0 3px;font-size:11px;color:#999;">🎟 Category</p>
                                <p style="margin:0;font-size:13px;font-weight:600;color:#111;">{d.TicketCategory} — ₦{d.TicketPrice:N0}</p>
                              </td>
                            </tr>
                            <tr>
                              <td width="50%">
                                <p style="margin:0 0 3px;font-size:11px;color:#999;">📍 Unit</p>
                                <p style="margin:0;font-size:13px;font-weight:600;color:#111;">{d.Unit}</p>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <!-- Perforated divider -->
                      <tr>
                        <td style="padding:0 16px;">
                          <table width="100%" cellpadding="0" cellspacing="0">
                            <tr>
                              <td style="border-top:2px dashed #e5e5e5;font-size:0;">&nbsp;</td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                      <!-- QR section -->
                      <tr>
                        <td style="padding:20px 16px 16px;">
                          <table cellpadding="0" cellspacing="0">
                            <tr>
                              <td style="padding-right:16px;vertical-align:top;">
                                <img src="{d.QRCodeUrl}" width="100" height="100"
                                     alt="Entry QR code"
                                     style="border:1px solid #e5e5e5;border-radius:8px;display:block;"/>
                              </td>
                              <td style="vertical-align:top;">
                                <p style="margin:0 0 4px;font-size:13px;font-weight:600;color:#111;font-family:monospace;">{d.QRCodeReference}</p>
                                <p style="margin:0 0 10px;font-size:12px;color:#777;line-height:1.5;">Present this QR code at the entrance. It is unique to your ticket and valid for one entry only.</p>
                                <span style="display:inline-block;font-size:11px;padding:3px 8px;border-radius:6px;background:#e8f5ee;color:#2e7d52;border:1px solid #b8dfc8;">✓ Valid · Single use</span>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>

                    </table>
                  </td>
                </tr>

                <!-- Order reference -->
                <tr>
                  <td style="padding:16px 32px;">
                    <table width="100%" cellpadding="0" cellspacing="0" style="background:#f9f9f9;border:1px solid #e5e5e5;border-radius:8px;">
                      <tr>
                        <td style="padding:12px 16px;">
                          <table width="100%" cellpadding="0" cellspacing="0">
                            <tr>
                              <td style="font-size:12px;color:#777;">Order reference</td>
                              <td align="right" style="font-size:12px;font-family:monospace;color:#111;">{d.OrderReference}</td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>

                <!-- Footer -->
                <tr>
                  <td style="padding:16px 32px 24px;border-top:1px solid #f0f0f0;background:#fafafa;">
                    <p style="margin:0;font-size:12px;color:#aaa;text-align:center;">
                      This QR code is unique to you — do not share it.<br/>
                      For support, contact <a href="mailto:admin@adexswagnation.com" style="color:#185FA5;">admin@adexswagnation.com</a>
                    </p>
                  </td>
                </tr>

              </table>
            </td></tr>
          </table>

        </body>
        </html>
        """;

    }
}
