﻿using APBD_Task_6.Models;
using System.Data.SqlClient;

namespace Zadanie5.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IConfiguration _configuration;

        public WarehouseService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<int>  AddProduct(ProductWarehouse productWarehouse)
        {
            var connectionString = _configuration.GetConnectionString("Database");
            using var connection = new SqlConnection(connectionString);
            using var cmd = new SqlCommand();

            cmd.Connection = connection;

            await connection.OpenAsync();
            cmd.CommandText = " SELECT TOP 1 [Order].IdOrder FROM [Order] " +
                "LEFT JOIN Product_Warehouse ON [Order].IdOrder = Product_Warehouse.IdOrder " +
                "WHERE [Order].IdProduct = @IdProduct " +
                "AND [Order].Amount = @Amount " +
                "AND Product_Warehouse.IdProductWarehouse IS NULL" +
                "AND [Order].CreatedAt < @CreatedAt";

            cmd.Parameters.AddWithValue("IdProduct", productWarehouse.IdProduct);
            cmd.Parameters.AddWithValue("Amount", productWarehouse.Amount);
            cmd.Parameters.AddWithValue("CreatedAt", productWarehouse.CreatedAt);

            var reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows) throw new Exception();

            await reader.ReadAsync();

            int idOrder = int.Parse(reader["IdOrder"].ToString());
            await reader.CloseAsync();

            cmd.Parameters.Clear();

            cmd.CommandText = "SELECT Proce from Product WHERE IdProduct = @IdProduct";
            cmd.Parameters.AddWithValue("IdProduct",productWarehouse.IdProduct);   

            reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows) throw new Exception();

            await reader.ReadAsync();
            double proce = double.Parse(reader["Price"].ToString());
            await reader.CloseAsync();

            cmd.Parameters.Clear();

            cmd.CommandText = "SELECT IdWarehouse FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            cmd.Parameters.AddWithValue("IdWarehouse", productWarehouse.IdWarehouse);

            reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows) throw new Exception();

            await reader.CloseAsync();

            cmd.Parameters.Clear();

            var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            cmd.Transaction = transaction;

            try
            {
                cmd.CommandText = "UPDATE [Order] SET FullfielledAt = @CreatedAt WHERE IdOrder = @IdOrder";
                cmd.Parameters.AddWithValue("CreatedAt", productWarehouse.CreatedAt);
                cmd.Parameters.AddWithValue("IdOrder", idOrder);

                int rowsUpdated = await cmd.ExecuteNonQueryAsync();

                if (rowsUpdated < 1) throw new Exception();

                cmd.Parameters.Clear();

                cmd.CommandText = "INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) " +
                    $"VALUSES(@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Amount*{proce}, @CreatedAt)";
                cmd.Parameters.AddWithValue("IdWarehouse", productWarehouse.IdWarehouse);
                cmd.Parameters.AddWithValue("IdOrder", idOrder);
                cmd.Parameters.AddWithValue("Amount", productWarehouse.Amount);
                cmd.Parameters.AddWithValue("CreatedAt", productWarehouse.CreatedAt);

                if (rowsUpdated < 1) throw new Exception();

                await transaction.CommitAsync();


            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw new Exception();
            }
            cmd.Parameters.Clear();

            cmd.CommandText = "SELECT TOP 1 IdProductWarehouse FROM Product_Warehouse ORDER BY IdProductWarehouse DESC";

            reader = await cmd.ExecuteReaderAsync();

            await reader.ReadAsync();

            int idProductWarehouse = int.Parse(reader["IdProductWarehouse"].ToString());

            await reader.CloseAsync();

            await connection.CloseAsync();

            return idProductWarehouse;

           
        }
    }
}
