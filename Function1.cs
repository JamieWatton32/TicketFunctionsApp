using System;
using System.Text.Json;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;



namespace TicketFunctionsApp {
    public class Function1 {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger) {
            _logger = logger;
        }

        [Function(nameof(Function1))]
        public async void Run([QueueTrigger("ticketqueue", Connection = "AzureWebJobsStorage")] QueueMessage message) {
            _logger.LogInformation($"C# Queue trigger function processed: {message.MessageText}");
            var options = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            };
            var ticket = JsonSerializer.Deserialize<Ticket>(message.MessageText, options);

            if (ticket == null) {
                throw new InvalidOperationException("Failed to deserialize the message into a Ticket Object.");
            }

            string? connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            using (SqlConnection connection = new SqlConnection(connectionString)) {
                await connection.OpenAsync();
                string query = @"
                INSERT INTO ConcertOrders 
                (TicketId, Email, Name, Phone, Quantity, CreditCard, Expiration, SecurityCode, 
                 Address, City, Province, PostalCode, Country) 
                VALUES 
                (@TicketId, @Email, @Name, @Phone, @Quantity, @CreditCard, @Expiration, @SecurityCode, 
                 @Address, @City, @Province, @PostalCode, @Country)";

                using (SqlCommand command = new SqlCommand(query, connection)) {
                    // Parameters
                    command.Parameters.AddWithValue("@TicketId", ticket.ConcertId);
                    command.Parameters.AddWithValue("@Email", ticket.Email);
                    command.Parameters.AddWithValue("@Name", ticket.Name);
                    command.Parameters.AddWithValue("@Phone", ticket.Phone);
                    command.Parameters.AddWithValue("@Quantity", ticket.Quantity);
                    command.Parameters.AddWithValue("@CreditCard", ticket.CreditCard);
                    command.Parameters.AddWithValue("@Expiration", ticket.Expiration);
                    command.Parameters.AddWithValue("@SecurityCode", ticket.SecurityCode);
                    command.Parameters.AddWithValue("@Address", ticket.Address);
                    command.Parameters.AddWithValue("@City", ticket.City);
                    command.Parameters.AddWithValue("@Province", ticket.Province);
                    command.Parameters.AddWithValue("@PostalCode", ticket.PostalCode);
                    command.Parameters.AddWithValue("@Country", ticket.Country);


                    command.ExecuteNonQuery();
                }


            }
        }

    }
}