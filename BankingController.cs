using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace BankingOperation
{
    [Route("api/[controller]")]
    [ApiController]
    public class BankingController : ControllerBase
    {
        private static ConcurrentDictionary<string, decimal> _accounts = new(new[]
      {
            new KeyValuePair<string, decimal>("ACC1001", 1000m),
            new KeyValuePair<string, decimal>("ACC1002", 2500m),
            new KeyValuePair<string, decimal>("ACC1003", 500m)
        });
        private static ConcurrentBag<string> _frozenAccounts = new() { "ACC1002" };
        [HttpPost("create-account")]
        public IActionResult CreateAccount([FromQuery] string accountId)
        {
            if (_accounts.ContainsKey(accountId))
                return BadRequest("Account already exists.");

            _accounts[accountId] = 0m; // Initial balance
            return Ok($"Account {accountId} created successfully.");
        }

        [HttpPost("deposit")]
        public IActionResult Deposit([FromQuery] string accountId, [FromQuery] decimal amount)
        {
            if (!_accounts.ContainsKey(accountId))
                return NotFound("Account not found.");

            _accounts[accountId] += amount;
            return Ok(new { accountId, balance = _accounts[accountId] });
        }

        [HttpPost("withdraw")]
        public IActionResult Withdraw([FromQuery] string accountId, [FromQuery] decimal amount)
        {
            if (amount <= 0)
                return BadRequest(new { error = "Withdrawal amount must be greater than zero" });

            if (amount > 10000)
                return BadRequest(new { error = "Exceeded maximum withdrawal limit per transaction (₹10,000)" });

            if (!_accounts.ContainsKey(accountId))
                return NotFound(new { error = "Account not found" });

            if (_frozenAccounts.Contains(accountId))
                return BadRequest(new { error = "Account is temporarily frozen. Withdrawals are disabled." });

            var currentBalance = _accounts[accountId];
            if (currentBalance < amount)
                return BadRequest(new { error = "Insufficient funds" });

            _accounts.AddOrUpdate(accountId, 0m, (key, oldBalance) => oldBalance - amount);

            return Ok(new { accountId, balance = _accounts[accountId] });
        }

        [HttpGet("balance")]
        public IActionResult GetBalance([FromQuery] string accountId)
        {
            if (!_accounts.ContainsKey(accountId))
                return NotFound("Account not found.");

            return Ok(new { accountId, balance = _accounts[accountId] });
        }

        [HttpPost("transfer")]
        public IActionResult Transfer([FromQuery] string fromAccountId, [FromQuery] string toAccountId, [FromQuery] decimal amount)
        {
            if (!_accounts.ContainsKey(fromAccountId) || !_accounts.ContainsKey(toAccountId))
                return NotFound("One or both accounts not found.");

            if (_accounts[fromAccountId] < amount)
                return BadRequest("Insufficient funds.");

            _accounts[fromAccountId] -= amount;
            _accounts[toAccountId] += amount;

            return Ok(new
            {
                fromAccountId,
                fromBalance = _accounts[fromAccountId],
                toAccountId,
                toBalance = _accounts[toAccountId]
            });
        }
        // New endpoint: Get all accounts
        [HttpGet("accounts")]
        public IActionResult GetAllAccounts()
        {
            return Ok(_accounts);
        }

        // New endpoint: Get account by ID
        [HttpGet("accounts/{accountId}")]
        public IActionResult GetAccountById(string accountId)
        {
            if (!_accounts.ContainsKey(accountId))
                return NotFound("Account not found.");

            return Ok(new { accountId, balance = _accounts[accountId] });
        }
    }
}
