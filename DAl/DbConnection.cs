using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace Inventory.DAL
{
    public class DbConnection
    {
        private readonly string _connectionString;
        public DbConnection(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}
