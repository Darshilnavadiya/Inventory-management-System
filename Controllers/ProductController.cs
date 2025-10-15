using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Inventory.Hubs;
using Inventory.Models;
using Inventory.DAL;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Inventory.Controllers
{
    public class ProductController : Controller
    {
        private readonly DbConnection _db;
        private readonly IHubContext<NotificationHub> _hub;

        public ProductController(IHubContext<NotificationHub> hub, IConfiguration configuration)
        {
            _hub = hub;
            _db = new DbConnection(configuration);
        }

        // ✅ List all products + Search
        public IActionResult Index(string search)
        {
            var products = new List<Product>();

            using (var conn = _db.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT * FROM Products", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new Product
                        {
                            ProductID = Convert.ToInt32(reader["ProductID"]),
                            ProductName = reader["ProductName"].ToString() ?? "",
                            Quantity = Convert.ToInt32(reader["Quantity"]),
                            Price = Convert.ToDecimal(reader["Price"])
                        });
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                products = products
                    .Where(p => p.ProductName.ToLower().Contains(search.ToLower()))
                    .ToList();
            }

            ViewBag.LowStockList = products.Where(p => p.Quantity <= 5).ToList();
            ViewBag.LowStockMessage = ViewBag.LowStockList.Count > 0 ? "⚠️ Low stock alert!" : null;
            ViewBag.SearchQuery = search;

            return View(products);
        }

        // ✅ Add Product (GET)
        public IActionResult Add() => View();

        // ✅ Add Product (POST)
        [HttpPost]
        public async Task<IActionResult> Add(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.ProductName) || product.Quantity <= 0)
            {
                ViewBag.Error = "Please enter a valid product name and quantity.";
                return View(product);
            }

            using (var conn = _db.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand(
                    "INSERT INTO Products (ProductName, Quantity, Price) VALUES (@name, @qty, @price)",
                    conn
                );
                cmd.Parameters.AddWithValue("@name", product.ProductName);
                cmd.Parameters.AddWithValue("@qty", product.Quantity);
                cmd.Parameters.AddWithValue("@price", product.Price);
                cmd.ExecuteNonQuery();
            }

            // Notify all users via SignalR
            await _hub.Clients.All.SendAsync("ReceiveNotification", $"New product added: {product.ProductName}");

            // Optional email notification (if EmailService is configured)
            // await EmailService.SendEmailAsync("darshil.inventory@gmail.com", "New Product Added",
            //     $"Product <b>{product.ProductName}</b> was added with quantity {product.Quantity}.");

            return RedirectToAction("Index");
        }

        // ✅ Edit Product (GET)
        public IActionResult Edit(int id)
        {
            Product product = null;

            using (var conn = _db.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT * FROM Products WHERE ProductID=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        product = new Product
                        {
                            ProductID = Convert.ToInt32(reader["ProductID"]),
                            ProductName = reader["ProductName"].ToString() ?? "",
                            Quantity = Convert.ToInt32(reader["Quantity"]),
                            Price = Convert.ToDecimal(reader["Price"])
                        };
                    }
                }
            }

            if (product == null)
                return NotFound();

            return View(product);
        }

        // ✅ Edit Product (POST)
        [HttpPost]
        public IActionResult Edit(Product product)
        {
            if (string.IsNullOrWhiteSpace(product.ProductName) || product.Quantity < 0)
            {
                ViewBag.Error = "Please enter a valid name and quantity.";
                return View(product);
            }

            using (var conn = _db.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand(
                    "UPDATE Products SET ProductName=@name, Quantity=@qty, Price=@price WHERE ProductID=@id",
                    conn
                );
                cmd.Parameters.AddWithValue("@name", product.ProductName);
                cmd.Parameters.AddWithValue("@qty", product.Quantity);
                cmd.Parameters.AddWithValue("@price", product.Price);
                cmd.Parameters.AddWithValue("@id", product.ProductID);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }

        // ✅ Delete Product
        public IActionResult Delete(int id)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand("DELETE FROM Products WHERE ProductID=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
    }
}
