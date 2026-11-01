using Microsoft.EntityFrameworkCore;
using WizardsEventScheduler.Api.Data;
using WizardsEventScheduler.Api.DTOs;
using WizardsEventScheduler.Api.Models;

namespace WizardsEventScheduler.Api.Services
{
    public class RegistrationResult
    {
        bool Success { get; set; }
        string Message { get; set; } = string.Empty;
        
        public static RegistrationResult Ok(string msg) => new() { Success = true, Message = msg };
        public static RegistrationResult Fail(string msg) => new() { Success = false, Message = msg };

        public bool IsSuccess => Success;
        public string ErrorMessage => Message;
    }
}