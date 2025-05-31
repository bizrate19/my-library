using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MyLibrary
{
    public partial class MainForm : Form
    {
        private string connectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=LibraryDB;Integrated Security=True;Connect Timeout=30"; // Changed to LibraryDB as per your SQL
        private int _selectedBookId = 0;
        private int _selectedBorrowerId = 0;

        public MainForm()
        {
            InitializeComponent();
            DtpIssueDate.Value = DateTime.Today;
            DtpDueDate.Value = DateTime.Today.AddDays(14);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadBooks();
            LoadBorrowers();
            LoadBooksForIssueComboBox();
            LoadBorrowersForIssueComboBox();
            LoadIssuedBooks(ChkShowReturned.Checked);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        #region Database Methods
        private int ExecuteNonQuery(string query, SqlParameter[] parameters = null)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    try
                    {
                        connection.Open();
                        return command.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return -1;
                    }
                }
            }
        }

        private object ExecuteScalar(string query, SqlParameter[] parameters = null)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    try
                    {
                        connection.Open();
                        return command.ExecuteScalar();
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return null;
                    }
                }
            }
        }

        private DataTable ExecuteQuery(string query, SqlParameter[] parameters = null)
        {
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    try
                    {
                        connection.Open();
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show("Database error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            return dataTable;
        }
        #endregion

        #region Books Management
        private void LoadBooks(string searchTerm = "")
        {
            string query = "SELECT BookID, Title, Author, Year, AvailableCopies FROM Books WHERE 1=1";
            SqlParameter[] parameters = null;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query += " AND (Title LIKE @SearchTerm OR Author LIKE @SearchTerm OR CAST(Year AS NVARCHAR(4)) LIKE @SearchTerm)";
                parameters = new SqlParameter[] { new SqlParameter("@SearchTerm", "%" + searchTerm + "%") };
            }

            DgvBooks.DataSource = ExecuteQuery(query, parameters);
        }

        private void DgvBooks_SelectionChanged(object sender, EventArgs e)
        {
            if (DgvBooks.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = DgvBooks.SelectedRows[0];
                _selectedBookId = Convert.ToInt32(selectedRow.Cells["BookID"].Value);
                TxtBookTitle.Text = selectedRow.Cells["Title"].Value.ToString();
                TxtBookAuthor.Text = selectedRow.Cells["Author"].Value.ToString();
                TxtBookYear.Text = selectedRow.Cells["Year"].Value.ToString();
                TxtBookCopies.Text = selectedRow.Cells["AvailableCopies"].Value.ToString();
            }
            else
            {
                ClearBookFields();
            }
        }

        private void ClearBookFields()
        {
            TxtBookTitle.Clear();
            TxtBookAuthor.Clear();
            TxtBookYear.Clear();
            TxtBookCopies.Clear();
            _selectedBookId = 0;
        }

        private void BtnClearBookFields_Click(object sender, EventArgs e)
        {
            ClearBookFields();
        }

        private void BtnAddBook_Click(object sender, EventArgs e)
        {
            if (!ValidateBookInput()) return;

            string query = "INSERT INTO Books (Title, Author, Year, AvailableCopies) VALUES (@Title, @Author, @Year, @AvailableCopies)";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Title", TxtBookTitle.Text.Trim()),
                new SqlParameter("@Author", TxtBookAuthor.Text.Trim()),
                new SqlParameter("@Year", int.Parse(TxtBookYear.Text.Trim())),
                new SqlParameter("@AvailableCopies", int.Parse(TxtBookCopies.Text.Trim()))
            };

            if (ExecuteNonQuery(query, parameters) > 0)
            {
                MessageBox.Show("Book added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadBooks();
                ClearBookFields();
            }
        }

        private void BtnEditBook_Click(object sender, EventArgs e)
        {
            if (_selectedBookId == 0)
            {
                MessageBox.Show("Please select a book to edit.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ValidateBookInput()) return;

            string query = "UPDATE Books SET Title = @Title, Author = @Author, Year = @Year, AvailableCopies = @AvailableCopies WHERE BookID = @BookID";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Title", TxtBookTitle.Text.Trim()),
                new SqlParameter("@Author", TxtBookAuthor.Text.Trim()),
                new SqlParameter("@Year", int.Parse(TxtBookYear.Text.Trim())),
                new SqlParameter("@AvailableCopies", int.Parse(TxtBookCopies.Text.Trim())),
                new SqlParameter("@BookID", _selectedBookId)
            };

            if (ExecuteNonQuery(query, parameters) > 0)
            {
                MessageBox.Show("Book updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadBooks();
                ClearBookFields();
            }
        }

        private void BtnDeleteBook_Click(object sender, EventArgs e)
        {
            if (_selectedBookId == 0)
            {
                MessageBox.Show("Please select a book to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Check if there are any issued books associated with this BookID
            string checkIssuedQuery = "SELECT COUNT(*) FROM IssuedBooks WHERE BookID = @BookID";
            SqlParameter[] checkIssuedParams = { new SqlParameter("@BookID", _selectedBookId) };
            int issuedCount = Convert.ToInt32(ExecuteScalar(checkIssuedQuery, checkIssuedParams));

            if (issuedCount > 0)
            {
                MessageBox.Show("This book cannot be deleted because it has associated issued records. Please ensure all copies are returned and no issue records exist for this book before attempting to delete.", "Deletion Restricted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                $"Are you sure you want to delete '{TxtBookTitle.Text}'?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                string deleteQuery = "DELETE FROM Books WHERE BookID = @BookID";
                SqlParameter[] parameters = { new SqlParameter("@BookID", _selectedBookId) };

                if (ExecuteNonQuery(deleteQuery, parameters) > 0)
                {
                    MessageBox.Show("Book deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadBooks();
                    ClearBookFields();
                }
            }
        }

        private bool ValidateBookInput()
        {
            if (string.IsNullOrWhiteSpace(TxtBookTitle.Text) ||
                string.IsNullOrWhiteSpace(TxtBookAuthor.Text) ||
                string.IsNullOrWhiteSpace(TxtBookYear.Text) ||
                string.IsNullOrWhiteSpace(TxtBookCopies.Text))
            {
                MessageBox.Show("All book fields are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!int.TryParse(TxtBookYear.Text, out int year) || year < 1000 || year > DateTime.Now.Year + 5)
            {
                MessageBox.Show("Please enter a valid publication year.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!int.TryParse(TxtBookCopies.Text, out int copies) || copies < 0)
            {
                MessageBox.Show("Please enter a valid number of available copies.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void TxtBookYear_TextChanged(object sender, EventArgs e)
        {
            // You can add real-time validation or formatting here if needed.
            // For example, to restrict input to only digits:
            // if (!System.Text.RegularExpressions.Regex.IsMatch(TxtBookYear.Text, "^[0-9]*$"))
            // {
            //     MessageBox.Show("Please enter only digits for the year.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //     TxtBookYear.Text = System.Text.RegularExpressions.Regex.Replace(TxtBookYear.Text, "[^0-9]", "");
            //     TxtBookYear.SelectionStart = TxtBookYear.Text.Length;
            // }
        }
        #endregion

        #region Borrowers Management
        private void LoadBorrowers(string searchTerm = "")
        {
            string query = "SELECT BorrowerID, Name, Email, Phone FROM Borrowers WHERE 1=1";
            SqlParameter[] parameters = null;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query += " AND (Name LIKE @SearchTerm OR Email LIKE @SearchTerm OR Phone LIKE @SearchTerm)";
                parameters = new SqlParameter[] { new SqlParameter("@SearchTerm", "%" + searchTerm + "%") };
            }

            DgvBorrowers.DataSource = ExecuteQuery(query, parameters);
        }

        private void DgvBorrowers_SelectionChanged(object sender, EventArgs e)
        {
            if (DgvBorrowers.SelectedRows.Count > 0)
            {
                DataGridViewRow selectedRow = DgvBorrowers.SelectedRows[0];
                _selectedBorrowerId = Convert.ToInt32(selectedRow.Cells["BorrowerID"].Value);
                TxtBorrowerName.Text = selectedRow.Cells["Name"].Value.ToString();
                TxtBorrowerEmail.Text = selectedRow.Cells["Email"].Value.ToString();
                TxtBorrowerPhone.Text = selectedRow.Cells["Phone"].Value.ToString();
            }
            else
            {
                ClearBorrowerFields();
            }
        }

        private void ClearBorrowerFields()
        {
            TxtBorrowerName.Clear();
            TxtBorrowerEmail.Clear();
            TxtBorrowerPhone.Clear();
            _selectedBorrowerId = 0;
        }

        private void BtnClearBorrowerFields_Click(object sender, EventArgs e)
        {
            ClearBorrowerFields();
        }

        private void BtnAddBorrower_Click(object sender, EventArgs e)
        {
            if (!ValidateBorrowerInput()) return;

            string query = "INSERT INTO Borrowers (Name, Email, Phone) VALUES (@Name, @Email, @Phone)";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Name", TxtBorrowerName.Text.Trim()),
                new SqlParameter("@Email", string.IsNullOrEmpty(TxtBorrowerEmail.Text.Trim()) ? (object)DBNull.Value : TxtBorrowerEmail.Text.Trim()),
                new SqlParameter("@Phone", string.IsNullOrEmpty(TxtBorrowerPhone.Text.Trim()) ? (object)DBNull.Value : TxtBorrowerPhone.Text.Trim())
            };

            if (ExecuteNonQuery(query, parameters) > 0)
            {
                MessageBox.Show("Borrower added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadBorrowers();
                ClearBorrowerFields();
            }
        }

        private void BtnEditBorrower_Click(object sender, EventArgs e)
        {
            if (_selectedBorrowerId == 0)
            {
                MessageBox.Show("Please select a borrower to edit.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ValidateBorrowerInput()) return;

            string query = "UPDATE Borrowers SET Name = @Name, Email = @Email, Phone = @Phone WHERE BorrowerID = @BorrowerID";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Name", TxtBorrowerName.Text.Trim()),
                new SqlParameter("@Email", string.IsNullOrEmpty(TxtBorrowerEmail.Text.Trim()) ? (object)DBNull.Value : TxtBorrowerEmail.Text.Trim()),
                new SqlParameter("@Phone", string.IsNullOrEmpty(TxtBorrowerPhone.Text.Trim()) ? (object)DBNull.Value : TxtBorrowerPhone.Text.Trim()),
                new SqlParameter("@BorrowerID", _selectedBorrowerId)
            };

            if (ExecuteNonQuery(query, parameters) > 0)
            {
                MessageBox.Show("Borrower updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadBorrowers();
                ClearBorrowerFields();
            }
        }

        private void BtnDeleteBorrower_Click(object sender, EventArgs e)
        {
            if (_selectedBorrowerId == 0)
            {
                MessageBox.Show("Please select a borrower to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Check if there are any issued books associated with this BorrowerID
            string checkIssuedQuery = "SELECT COUNT(*) FROM IssuedBooks WHERE BorrowerID = @BorrowerID";
            SqlParameter[] checkIssuedParams = { new SqlParameter("@BorrowerID", _selectedBorrowerId) };
            int issuedCount = Convert.ToInt32(ExecuteScalar(checkIssuedQuery, checkIssuedParams));

            if (issuedCount > 0)
            {
                MessageBox.Show("This borrower cannot be deleted because they have associated issued book records. Please ensure all books borrowed by this person are returned and their issue records are cleared before attempting to delete.", "Deletion Restricted", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                $"Are you sure you want to delete '{TxtBorrowerName.Text}'?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                string deleteQuery = "DELETE FROM Borrowers WHERE BorrowerID = @BorrowerID";
                SqlParameter[] parameters = { new SqlParameter("@BorrowerID", _selectedBorrowerId) };

                if (ExecuteNonQuery(deleteQuery, parameters) > 0)
                {
                    MessageBox.Show("Borrower deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadBorrowers();
                    ClearBorrowerFields();
                }
            }
        }

        private bool ValidateBorrowerInput()
        {
            if (string.IsNullOrWhiteSpace(TxtBorrowerName.Text))
            {
                MessageBox.Show("Borrower name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string email = TxtBorrowerEmail.Text.Trim();
            if (!string.IsNullOrEmpty(email) && !IsValidEmail(email))
            {
                MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Issued Books Management
        private void LoadBooksForIssueComboBox()
        {
            string query = "SELECT BookID, Title FROM Books WHERE AvailableCopies > 0 ORDER BY Title";
            DataTable dt = ExecuteQuery(query);
            CmbBooks.DataSource = dt;
            CmbBooks.DisplayMember = "Title";
            CmbBooks.ValueMember = "BookID";
            CmbBooks.SelectedIndex = -1;
        }

        private void LoadBorrowersForIssueComboBox()
        {
            string query = "SELECT BorrowerID, Name FROM Borrowers ORDER BY Name";
            DataTable dt = ExecuteQuery(query);
            CmbBorrowers.DataSource = dt;
            CmbBorrowers.DisplayMember = "Name";
            CmbBorrowers.ValueMember = "BorrowerID";
            CmbBorrowers.SelectedIndex = -1;
        }

        private void LoadIssuedBooks(bool showReturned, string searchTerm = "")
        {
            string query = @"
                SELECT
                    IB.IssueID,
                    B.Title AS BookTitle,
                    Br.Name AS BorrowerName,
                    IB.IssueDate,
                    IB.DueDate,
                    IB.Returned,
                    -- Removed ReturnDate as it's not in the SQL schema
                    B.BookID,
                    Br.BorrowerID
                FROM IssuedBooks IB
                JOIN Books B ON IB.BookID = B.BookID
                JOIN Borrowers Br ON IB.BorrowerID = Br.BorrowerID
                WHERE 1=1";

            if (!showReturned)
            {
                query += " AND IB.Returned = 0";
            }

            SqlParameter[] parameters = null;
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query += " AND (B.Title LIKE @SearchTerm OR Br.Name LIKE @SearchTerm)";
                parameters = new SqlParameter[] { new SqlParameter("@SearchTerm", "%" + searchTerm + "%") };
            }

            query += " ORDER BY IB.IssueDate DESC";

            DataTable dt = ExecuteQuery(query, parameters);
            DgvIssuedBooks.DataSource = dt;

            if (DgvIssuedBooks.Columns.Contains("BookID")) DgvIssuedBooks.Columns["BookID"].Visible = false;
            if (DgvIssuedBooks.Columns.Contains("BorrowerID")) DgvIssuedBooks.Columns["BorrowerID"].Visible = false;
            if (DgvIssuedBooks.Columns.Contains("Returned")) DgvIssuedBooks.Columns["Returned"].Visible = false;
            // No longer checking for or hiding "ReturnDate" column

            UpdateReturnButtonState();
        }

        private void UpdateReturnButtonState()
        {
            BtnReturnBook.Enabled = (DgvIssuedBooks.SelectedRows.Count > 0 &&
                                       DgvIssuedBooks.SelectedRows[0].Cells["Returned"].Value != null &&
                                       Convert.ToBoolean(DgvIssuedBooks.SelectedRows[0].Cells["Returned"].Value) == false);
        }

        private void BtnIssueBook_Click(object sender, EventArgs e)
        {
            if (CmbBooks.SelectedValue == null || CmbBorrowers.SelectedValue == null)
            {
                MessageBox.Show("Please select both a book and a borrower.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int bookId = Convert.ToInt32(CmbBooks.SelectedValue);
            int borrowerId = Convert.ToInt32(CmbBorrowers.SelectedValue);
            DateTime issueDate = DtpIssueDate.Value.Date;
            DateTime dueDate = DtpDueDate.Value.Date;

            if (dueDate < issueDate)
            {
                MessageBox.Show("Due date cannot be before issue date.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    string updateBookQuery = "UPDATE Books SET AvailableCopies = AvailableCopies - 1 WHERE BookID = @BookID AND AvailableCopies > 0";
                    SqlParameter[] updateBookParams = { new SqlParameter("@BookID", bookId) };
                    SqlCommand updateBookCmd = new SqlCommand(updateBookQuery, connection, transaction);
                    updateBookCmd.Parameters.AddRange(updateBookParams);

                    if (updateBookCmd.ExecuteNonQuery() == 0)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Book is not available for issue.", "Issue Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Insert into IssuedBooks without ReturnDate
                    string insertIssuedQuery = "INSERT INTO IssuedBooks (BookID, BorrowerID, IssueDate, DueDate, Returned) VALUES (@BookID, @BorrowerID, @IssueDate, @DueDate, 0)";
                    SqlParameter[] insertIssuedParams = new SqlParameter[]
                    {
                        new SqlParameter("@BookID", bookId),
                        new SqlParameter("@BorrowerID", borrowerId),
                        new SqlParameter("@IssueDate", issueDate),
                        new SqlParameter("@DueDate", dueDate)
                    };
                    SqlCommand insertIssuedCmd = new SqlCommand(insertIssuedQuery, connection, transaction);
                    insertIssuedCmd.Parameters.AddRange(insertIssuedParams);
                    insertIssuedCmd.ExecuteNonQuery();

                    transaction.Commit();
                    MessageBox.Show("Book issued successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    LoadBooksForIssueComboBox();
                    LoadBooks();
                    LoadIssuedBooks(ChkShowReturned.Checked);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Error during issue: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnReturnBook_Click(object sender, EventArgs e)
        {
            if (DgvIssuedBooks.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select an issued book to return.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataGridViewRow selectedRow = DgvIssuedBooks.SelectedRows[0];
            int issueId = Convert.ToInt32(selectedRow.Cells["IssueID"].Value);
            int bookId = Convert.ToInt32(selectedRow.Cells["BookID"].Value);
            bool isReturned = Convert.ToBoolean(selectedRow.Cells["Returned"].Value);

            if (isReturned)
            {
                MessageBox.Show("This book has already been returned.", "Already Returned", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show("Are you sure you want to mark this book as returned?", "Confirm Return", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();

                    try
                    {
                        string updateBookQuery = "UPDATE Books SET AvailableCopies = AvailableCopies + 1 WHERE BookID = @BookID";
                        SqlCommand updateBookCmd = new SqlCommand(updateBookQuery, connection, transaction);
                        updateBookCmd.Parameters.Add(new SqlParameter("@BookID", bookId));
                        updateBookCmd.ExecuteNonQuery();

                        // Update IssuedBooks table to mark as returned, but without ReturnDate
                        string updateIssuedQuery = "UPDATE IssuedBooks SET Returned = 1 WHERE IssueID = @IssueID";
                        SqlCommand updateIssuedCmd = new SqlCommand(updateIssuedQuery, connection, transaction);
                        updateIssuedCmd.Parameters.Add(new SqlParameter("@IssueID", issueId));
                        updateIssuedCmd.ExecuteNonQuery();

                        transaction.Commit();
                        MessageBox.Show("Book returned successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        LoadBooksForIssueComboBox();
                        LoadBooks();
                        LoadIssuedBooks(ChkShowReturned.Checked);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Error during return: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void DgvIssuedBooks_SelectionChanged(object sender, EventArgs e)
        {
            UpdateReturnButtonState();
        }

        private void ChkShowReturned_CheckedChanged(object sender, EventArgs e)
        {
            LoadIssuedBooks(ChkShowReturned.Checked);
        }
        #endregion

        private void TabControlMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (TabControlMain.SelectedTab == TabPageBooks)
            {
                LoadBooks();
                ClearBookFields();
            }
            else if (TabControlMain.SelectedTab == TabPageBorrowers)
            {
                LoadBorrowers();
                ClearBorrowerFields();
            }
            else if (TabControlMain.SelectedTab == TabPageIssuedBooks)
            {
                LoadBooksForIssueComboBox();
                LoadBorrowersForIssueComboBox();
                LoadIssuedBooks(ChkShowReturned.Checked);
                CmbBooks.SelectedIndex = -1;
                CmbBorrowers.SelectedIndex = -1;
                DtpIssueDate.Value = DateTime.Today;
                DtpDueDate.Value = DateTime.Today.AddDays(14);
            }
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            this.Hide();
            // Assuming LoginForm has a parameterless constructor.
            // If LoginForm requires arguments, you will need to provide them here.
            LoginForm loginForm = new LoginForm();
            loginForm.Show();
        }
    }
}
