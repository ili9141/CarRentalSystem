using CarRental.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows.Forms;

namespace CarRental.Admin
{
    public class LoginForm : Form
    {
        private TextBox txtEmail;
        private TextBox txtPassword;
        private Button btnLogin;
        private Button btnCancel;
        private Label lblError;

        public LoginForm()
        {
            InitializeForm();
        }

        private void InitializeForm()
        {
            // Form settings
            this.Text = "Admin Login";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Title
            var lblTitle = new Label
            {
                Text = "CAR RENTAL ADMIN",
                Font = new Font("Arial", 16, FontStyle.Bold),
                Location = new Point(60, 30),
                AutoSize = true
            };

            // Email field
            var lblEmail = new Label
            {
                Text = "Email:",
                Location = new Point(50, 90),
                Size = new Size(100, 25)
            };

            txtEmail = new TextBox
            {
                Location = new Point(150, 87),
                Size = new Size(200, 25),
                
            };

            // Password field
            var lblPassword = new Label
            {
                Text = "Password:",
                Location = new Point(50, 130),
                Size = new Size(100, 25)
            };

            txtPassword = new TextBox
            {
                Location = new Point(150, 127),
                Size = new Size(200, 25),
                PasswordChar = '*',
              
            };

            // Error Label
            lblError = new Label
            {
                Text = "",
                ForeColor = Color.Red,
                Location = new Point(50, 165),
                Size = new Size(300, 25),
                Visible = false
            };

            // Login button
            btnLogin = new Button
            {
                Text = "LOGIN",
                Location = new Point(150, 200),
                Size = new Size(100, 40),
                BackColor = Color.Blue,
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            btnLogin.Click += BtnLogin_Click;

            // Cancel button
            btnCancel = new Button
            {
                Text = "CANCEL",
                Location = new Point(260, 200),
                Size = new Size(100, 40),
                BackColor = Color.Gray,
                ForeColor = Color.White
            };
            btnCancel.Click += (s, e) => Application.Exit();

            // Add all controls
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblEmail);
            this.Controls.Add(txtEmail);
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(lblError);
            this.Controls.Add(btnLogin);
            this.Controls.Add(btnCancel);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            lblError.Visible = false;

            // Validation
            if (string.IsNullOrEmpty(email))
            {
                lblError.Text = "Please enter email";
                lblError.Visible = true;
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                lblError.Text = "Please enter password";
                lblError.Visible = true;
                return;
            }

            try
            {
                // Create DbContext
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlServer("Server=localhost;Database=CarRentalDB;Trusted_Connection=True;TrustServerCertificate=True;")
                    .Options;

                using (var context = new ApplicationDbContext(options))
                {
                    // Find admin by email
                    var admin = context.AdminUsers.FirstOrDefault(a => a.Email.ToLower() == email.ToLower());

                    if (admin == null)
                    {
                        lblError.Text = "No admin account found";
                        lblError.Visible = true;
                        return;
                    }

                    // SECURE: Verify password with BCrypt
                    bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash);

                    if (isPasswordValid)
                    {
                        // Success - open MainForm
                        OpenMainForm(admin.Id);
                    }
                    else
                    {
                        lblError.Text = "Invalid password";
                        lblError.Visible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                lblError.Text = $"Login failed: {ex.Message}";
                lblError.Visible = true;
            }
        }

        private void OpenMainForm(int adminId)
        {
            // Create fresh context for MainForm
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=localhost;Database=CarRentalDB;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            var context = new ApplicationDbContext(options);

            // Create and show MainForm
            var mainForm = new MainForm(context, adminId);

            // Handle MainForm closing
            mainForm.FormClosed += (s, e) =>
            {
                context?.Dispose();
                Application.Exit();
            };

            mainForm.Show();
            this.Hide();
        }
    }
}