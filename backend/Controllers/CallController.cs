using System.ComponentModel.DataAnnotations;
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

        public CallController(IUserRepository userRepo, IHubContext<CallNotificationHub> hubContext, IConfiguration config)
        {
            _userRepo = userRepo;
            _hubContext = hubContext;
            _config = config;
        }

        [HttpPost("/api/call/incoming")]
        public async Task<TwiMLResult> Incoming([FromForm] string From, [FromForm] string CallSid)
        {
            var response = new VoiceResponse();
            try
            {
                var phoneNumber = From.Replace("+", "").Trim();
                var user = await _userRepo.GetByPhoneNumberAsync(phoneNumber);

                // If user does not exist, create and save them.
                if (user == null)
                {
                    user = new User
                    {
                        FullName = "Unknown Caller",
                        PhoneNumber = phoneNumber,
                        Email = "N/A",
                        Address = "N/A",
                        CreatedAt = DateTime.UtcNow // Assuming your User model has this property
                    };
                    // 1. FIX: Use a synchronous Add method (standard practice).
                    _userRepo.Add(user); 
                    await _userRepo.SaveChangesAsync(); // This saves them to the database.
                }
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    response.Say("Invalid phone number received.", voice: "alice");
                    return TwiML(response);
                }

                Console.WriteLine($"Incoming call from: {phoneNumber}, CallSid: {CallSid}");
                
        
                await _hubContext.Clients.All.SendAsync("IncomingCall", user);

                


                // Generate the TwiML response
                response.Say("Thank you for calling. Connecting you to an agent now.", voice: "alice");

                var start = new Start();
                start.Stream(url: "wss://" + Request.Host + "/audiostream");
                response.Append(start);

                var agentNumber = _config["Twilio:AgentPhoneNumber"];
                response.Dial(agentNumber);

                // 2. FIX: Reduce the pause to a reasonable length.
                response.Pause(length: 1);
            }
            catch (Exception ex)
            {
                // Log the exception (ex) with your logging framework.
                Console.WriteLine($"An error occurred in the incoming call webhook: {ex.Message}");
                response.Say("We're sorry, an error occurred. Please try again later.", voice: "alice");
            }

            return TwiML(response);
        }
    }
}