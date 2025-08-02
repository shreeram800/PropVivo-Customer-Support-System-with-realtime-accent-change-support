using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Twilio.AspNet.Core;
using Twilio.TwiML;
using Twilio.TwiML.Voice;


namespace RealtimeAccentTransformer.Controllers
{
    public class CallController : TwilioController
    {
        private readonly IUserRepository _userRepo;
        private readonly IHubContext<CallNotificationHub> _hubContext;
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;

        public CallController(
            IUserRepository userRepo,
            IHubContext<CallNotificationHub> hubContext,
            IConfiguration config,
            AppDbContext db)
        {
            _userRepo = userRepo;
            _hubContext = hubContext;
            _config = config;
            _db = db;
        }

                [HttpPost("/api/call/incoming")]
        public async Task<TwiMLResult> Incoming([FromForm] string From, [FromForm] string CallSid)
        {
            var response = new VoiceResponse();
            try
            {
                var phoneNumber = From.Replace("+", "").Trim();
                var user = await _userRepo.GetByPhoneNumberAsync(phoneNumber);

                // Create user if not exists
                if (user == null)
                {
                    user = new User
                    {
                        FullName = "Unknown Caller",
                        PhoneNumber = phoneNumber,
                        Email = "N/A",
                        Address = "N/A",
                        CreatedAt = DateTime.UtcNow
                    };
                    _userRepo.Add(user);
                    await _userRepo.SaveChangesAsync();
                }

                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    response.Say("Invalid phone number received.", voice: "alice");
                    return TwiML(response);
                }

                Console.WriteLine($"Incoming call from: {phoneNumber}, CallSid: {CallSid}");

                // 1. Create a Call record using updated fields
                var call = new Call
                {
                    CustomerId = user.Id,
                    user = user,
                    CallStartTime = DateTime.UtcNow,
                    Status = "Connected",
                    IsVoiceModulationUsed = true,
                    AgentName = "Auto-Assigned", // Or use lookup if dynamic
                    CallRecordingUrl = "", // Twilio can post this later via webhook
                };

                _db.Calls.Add(call);
                await _db.SaveChangesAsync();

                // 2. Notify frontend (optional)
                await _hubContext.Clients.All.SendAsync("IncomingCall", user);

                // 3. TwiML with streaming to audio pipeline
                response.Say("Thank you for calling. Connecting you to an agent now.", voice: "alice");

                var streamUrl = $"wss://{Request.Host}/audiostream?callId={call.Id}";
                var start = new Start();
                start.Stream(url: streamUrl);
                response.Append(start);

                var agentNumber = _config["Twilio:AgentPhoneNumber"];
                response.Dial(agentNumber);

                response.Pause(length: 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Call webhook error: {ex.Message}");
                response.Say("We're sorry, an error occurred. Please try again later.", voice: "alice");
            }

            return TwiML(response);
        }

    }
}
