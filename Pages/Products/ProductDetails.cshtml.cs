using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NextCommerce.Pages.Products
{
    public class ProductDetailsModel : BasePageModel
    {
        public ProductDetailsModel(IConfiguration configuration) : base(configuration)
        {
            Reviews = new List<ReviewInfo>();
            UserOrders = new List<OrderInfo>();
        }

        public ProductInfo Product { get; set; }
        public List<ReviewInfo> Reviews { get; set; }
        public List<OrderInfo> UserOrders { get; set; }
        public bool HasUserReviewed { get; set; }
        public bool HasPurchasedProduct { get; set; }
        public int UnreviewedOrderCount { get; set; }

        public void OnGet(int id)
        {
            LoadProduct(id);
            LoadReviews(id);
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (userId.HasValue)
            {
                CheckUserReview(id);
                CheckUserPurchaseHistory(id, userId.Value);
            }
        }

        public IActionResult OnPostAddToCart(int productId, int quantity)
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Check product availability
                            var checkCommand = new SqlCommand(
                                "SELECT Quantity FROM Products WHERE Id = @ProductId",
                                connection, transaction);
                            checkCommand.Parameters.AddWithValue("@ProductId", productId);
                            var availableQuantity = (int)checkCommand.ExecuteScalar();

                            if (quantity > availableQuantity)
                            {
                                TempData["ErrorMessage"] = "Requested quantity not available.";
                                return RedirectToPage(new { id = productId });
                            }

                            // Check if item already exists in cart
                            var cartCommand = new SqlCommand(
                                "SELECT Quantity FROM Cart WHERE UserId = @UserId AND ProductId = @ProductId",
                                connection, transaction);
                            cartCommand.Parameters.AddWithValue("@UserId", userId.Value);
                            cartCommand.Parameters.AddWithValue("@ProductId", productId);
                            var existingQuantity = cartCommand.ExecuteScalar();

                            if (existingQuantity != null)
                            {
                                // Update existing cart item
                                var updateCommand = new SqlCommand(
                                    "UPDATE Cart SET Quantity = Quantity + @Quantity WHERE UserId = @UserId AND ProductId = @ProductId",
                                    connection, transaction);
                                updateCommand.Parameters.AddWithValue("@UserId", userId.Value);
                                updateCommand.Parameters.AddWithValue("@ProductId", productId);
                                updateCommand.Parameters.AddWithValue("@Quantity", quantity);
                                updateCommand.ExecuteNonQuery();
                            }
                            else
                            {
                                // Insert new cart item
                                var insertCommand = new SqlCommand(
                                    "INSERT INTO Cart (UserId, ProductId, Quantity) VALUES (@UserId, @ProductId, @Quantity)",
                                    connection, transaction);
                                insertCommand.Parameters.AddWithValue("@UserId", userId.Value);
                                insertCommand.Parameters.AddWithValue("@ProductId", productId);
                                insertCommand.Parameters.AddWithValue("@Quantity", quantity);
                                insertCommand.ExecuteNonQuery();
                            }

                            // Update product quantity
                            var updateProductCommand = new SqlCommand(
                                "UPDATE Products SET Quantity = Quantity - @Quantity WHERE Id = @ProductId",
                                connection, transaction);
                            updateProductCommand.Parameters.AddWithValue("@ProductId", productId);
                            updateProductCommand.Parameters.AddWithValue("@Quantity", quantity);
                            updateProductCommand.ExecuteNonQuery();

                            transaction.Commit();
                            TempData["SuccessMessage"] = "Item added to cart successfully.";
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            TempData["ErrorMessage"] = "Error adding item to cart: " + ex.Message;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error adding item to cart: " + ex.Message;
            }

            return RedirectToPage(new { id = productId });
        }

        public IActionResult OnPostDelete(int productId)
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (!userId.HasValue || !IsAdmin)
            {
                return RedirectToPage("/Login");
            }

            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // First remove from any active carts
                            var deleteCartCommand = new SqlCommand(
                                "DELETE FROM Cart WHERE ProductId = @ProductId",
                                connection, transaction);
                            deleteCartCommand.Parameters.AddWithValue("@ProductId", productId);
                            deleteCartCommand.ExecuteNonQuery();

                            // Then soft delete the product
                            var updateProductCommand = new SqlCommand(
                                "UPDATE Products SET IsDeleted = 1 WHERE Id = @ProductId",
                                connection, transaction);
                            updateProductCommand.Parameters.AddWithValue("@ProductId", productId);
                            updateProductCommand.ExecuteNonQuery();

                            transaction.Commit();
                            TempData["SuccessMessage"] = "Product deleted successfully.";
                            return RedirectToPage("/Products/Products");  // Redirect back to products list
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            TempData["ErrorMessage"] = "Error deleting product: " + ex.Message;
                            return RedirectToPage(new { id = productId });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting product: " + ex.Message;
                return RedirectToPage(new { id = productId });
            }
        }

        public IActionResult OnPostAddReview(int productId, int orderId, int rating, string? comment)
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Login");
            }

            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Invalid rating value. Please select 1-5 stars.";
                return RedirectToPage(new { id = productId });
            }

            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Verify the order exists and belongs to the user
                            var verifyCommand = new SqlCommand(
                                @"SELECT COUNT(1) 
                                  FROM Orders o
                                  JOIN OrderItems oi ON o.Id = oi.OrderId
                                  WHERE o.Id = @OrderId 
                                    AND o.UserId = @UserId 
                                    AND oi.ProductId = @ProductId
                                    AND o.Status = 'Completed'",
                                connection, transaction);
                            verifyCommand.Parameters.AddWithValue("@OrderId", orderId);
                            verifyCommand.Parameters.AddWithValue("@UserId", userId.Value);
                            verifyCommand.Parameters.AddWithValue("@ProductId", productId);

                            if ((int)verifyCommand.ExecuteScalar() == 0)
                            {
                                TempData["ErrorMessage"] = "Invalid order or product combination.";
                                return RedirectToPage(new { id = productId });
                            }

                            // Check if order already has a review for this product
                            var checkCommand = new SqlCommand(
                                @"SELECT COUNT(1) 
                                  FROM ProductReviews 
                                  WHERE ProductId = @ProductId 
                                    AND UserId = @UserId
                                    AND OrderId = @OrderId",
                                connection, transaction);
                            checkCommand.Parameters.AddWithValue("@ProductId", productId);
                            checkCommand.Parameters.AddWithValue("@UserId", userId.Value);
                            checkCommand.Parameters.AddWithValue("@OrderId", orderId);

                            if ((int)checkCommand.ExecuteScalar() > 0)
                            {
                                TempData["ErrorMessage"] = "You have already reviewed this product for this order.";
                                return RedirectToPage(new { id = productId });
                            }

                            // Add the new review
                            var command = new SqlCommand(
                                @"INSERT INTO ProductReviews (ProductId, UserId, OrderId, Rating, Comment) 
                                  VALUES (@ProductId, @UserId, @OrderId, @Rating, @Comment)",
                                connection, transaction);
                            command.Parameters.AddWithValue("@ProductId", productId);
                            command.Parameters.AddWithValue("@UserId", userId.Value);
                            command.Parameters.AddWithValue("@OrderId", orderId);
                            command.Parameters.AddWithValue("@Rating", rating);
                            command.Parameters.AddWithValue("@Comment", (object?)comment ?? DBNull.Value);
                            command.ExecuteNonQuery();

                            // Update the average rating
                            var updateAverageCommand = new SqlCommand(
                                @"UPDATE Products 
                                  SET AverageRating = (
                                      SELECT AVG(CAST(Rating AS FLOAT))
                                      FROM ProductReviews
                                      WHERE ProductId = @ProductId
                                  )
                                  WHERE Id = @ProductId",
                                connection, transaction);
                            updateAverageCommand.Parameters.AddWithValue("@ProductId", productId);
                            updateAverageCommand.ExecuteNonQuery();

                            transaction.Commit();
                            TempData["SuccessMessage"] = "Review added successfully.";
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            TempData["ErrorMessage"] = "Error adding review: " + ex.Message;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error adding review: " + ex.Message;
            }

            return RedirectToPage(new { id = productId });
        }

        private void LoadProduct(int id)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand(
                    @"SELECT p.Id, p.Name, p.Description, p.Price, p.Quantity, p.Image, 
                             p.DateCreated, c.Name as CategoryName, c.Id as CategoryId,
                             ISNULL(AVG(CAST(pr.Rating as FLOAT)), 0) as AverageRating,
                             COUNT(pr.Id) as ReviewCount
                      FROM Products p
                      LEFT JOIN Category c ON p.CategoryId = c.Id
                      LEFT JOIN ProductReviews pr ON p.Id = pr.ProductId
                      WHERE p.Id = @ProductId AND p.IsDeleted = 0
                      GROUP BY p.Id, p.Name, p.Description, p.Price, p.Quantity, p.Image, 
                               p.DateCreated, c.Name, c.Id",
                    connection);
                command.Parameters.AddWithValue("@ProductId", id);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Product = new ProductInfo
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Description = reader.GetString(2),
                            Price = reader.GetDecimal(3),
                            Quantity = reader.GetInt32(4),
                            Image = reader.IsDBNull(5) ? null : reader.GetString(5),
                            DateCreated = reader.GetDateTime(6),
                            CategoryName = reader.IsDBNull(7) ? null : reader.GetString(7),
                            CategoryId = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                            AverageRating = Math.Round(reader.GetDouble(9), 1),
                            ReviewCount = reader.GetInt32(10)
                        };
                    }
                }
            }
        }

        private void LoadReviews(int productId)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand(
                    @"SELECT pr.Id, pr.Rating, pr.Comment, pr.DateCreated, 
                             u.Username as UserName
                      FROM ProductReviews pr
                      JOIN Users u ON pr.UserId = u.Id
                      WHERE pr.ProductId = @ProductId
                      ORDER BY pr.DateCreated DESC",
                    connection);
                command.Parameters.AddWithValue("@ProductId", productId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Reviews.Add(new ReviewInfo
                        {
                            Id = reader.GetInt32(0),
                            Rating = reader.GetInt32(1),
                            Comment = reader.IsDBNull(2) ? null : reader.GetString(2),
                            DateCreated = reader.GetDateTime(3),
                            UserName = reader.GetString(4)
                        });
                    }
                }
            }
        }

        private void CheckUserReview(int productId)
        {
            var userId = HttpContext.Session.GetInt32("LoggedUser");
            if (userId.HasValue)
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    var command = new SqlCommand(
                        "SELECT COUNT(1) FROM ProductReviews WHERE ProductId = @ProductId AND UserId = @UserId",
                        connection);
                    command.Parameters.AddWithValue("@ProductId", productId);
                    command.Parameters.AddWithValue("@UserId", userId.Value);
                    HasUserReviewed = ((int)command.ExecuteScalar()) > 0;
                }
            }
        }

        private void CheckUserPurchaseHistory(int productId, int userId)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                connection.Open();
                var command = new SqlCommand(
                    @"SELECT o.Id, o.OrderNumber, o.OrderDate,
                             COUNT(pr.OrderId) as ReviewCount
                      FROM Orders o
                      JOIN OrderItems oi ON o.Id = oi.OrderId
                      LEFT JOIN ProductReviews pr ON pr.OrderId = o.Id 
                        AND pr.ProductId = oi.ProductId
                      WHERE o.UserId = @UserId 
                        AND oi.ProductId = @ProductId
                        AND o.Status = 'Completed'
                      GROUP BY o.Id, o.OrderNumber, o.OrderDate
                      HAVING COUNT(pr.OrderId) = 0",  // Only get orders that haven't been reviewed
                    connection);
                
                command.Parameters.AddWithValue("@UserId", userId);
                command.Parameters.AddWithValue("@ProductId", productId);

                using (var reader = command.ExecuteReader())
                {
                    UserOrders.Clear();
                    while (reader.Read())
                    {
                        UserOrders.Add(new OrderInfo
                        {
                            Id = reader.GetInt32(0),
                            OrderNumber = reader.GetString(1),
                            OrderDate = reader.GetDateTime(2)
                        });
                    }
                }
            }

            HasPurchasedProduct = UserOrders.Any();
            UnreviewedOrderCount = UserOrders.Count;
        }

        public class ReviewInfo
        {
            public int Id { get; set; }
            public int Rating { get; set; }
            public string? Comment { get; set; }
            public DateTime DateCreated { get; set; }
            public string UserName { get; set; }
        }

        public class ProductInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public string Image { get; set; }
            public DateTime DateCreated { get; set; }
            public string CategoryName { get; set; }
            public int CategoryId { get; set; }
            public double AverageRating { get; set; }
            public int ReviewCount { get; set; }
        }

        public class OrderInfo
        {
            public int Id { get; set; }
            public string OrderNumber { get; set; }
            public DateTime OrderDate { get; set; }
        }
    }
} 