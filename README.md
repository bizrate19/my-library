# My Library Management System

This is a simple desktop application for managing a library's books, borrowers, and issued books. It provides functionalities for:

* **User Authentication:** Secure login for library staff.
* **Book Management:** Add, edit, and delete book records.
* **Borrower Management:** Add, edit, and delete borrower records.
* **Book Issuing and Returning:** Track borrowed books and their due dates.

## Features

* **Login System:**
    * User login with username and password.
    * Success/failure messages for login attempts.

* **Books Section:**
    * Display a list of books with BookID, Title, Author, Year, and Available Copies.
    * Add new books to the library.
    * Edit existing book details.
    * Delete book records (with a confirmation if no book is selected).
    * Update book information (e.g., available copies).

* **Borrowers Section:**
    * Display a list of borrowers with BorrowerID, Name, Email, and Phone.
    * Add new borrower records.
    * Edit existing borrower details.
    * Delete borrower records.
    * Update borrower information.

* **Issued Books Section:**
    * Display a list of issued books with IssueID, Book Title, Borrower Name, Issue Date, and Due Date.
    * Issue books to borrowers.
    * Return books (with confirmation).
    * Option to show returned books (though not explicitly shown in the provided images, it's a common feature for such systems).

## Technologies Used

Based on the provided screenshots, the application appears to be a Windows Forms (WinForms) application developed with C#.

* **Frontend:** C# WinForms
* **Backend/Logic:** C#
* **Database:** (Not explicitly shown, but likely uses a local database like SQL Server Compact, SQLite, or a connected SQL Server instance for data persistence.)

## Setup and Installation

1.  **Prerequisites:**
    * Visual Studio (2019 or later recommended) with .NET desktop development workload.

2.  **Clone the Repository:**
    ```bash
    git clone [repository_url]
    ```
    (Replace `[repository_url]` with the actual URL of your Git repository.)

3.  **Open in Visual Studio:**
    * Open the `.sln` file (solution file) in Visual Studio.

4.  **Restore NuGet Packages:**
    * Right-click on the solution in Solution Explorer and select "Restore NuGet Packages" if prompted or if there are build errors related to missing packages.

5.  **Database Setup (if applicable):**
    * If the application uses a local database file (e.g., `.mdf`, `.db`), ensure it's included in the project and the connection string is correctly configured in `App.config` or `web.config` (if it were a web app).
    * If it connects to an external database, you'll need to set up the database schema and ensure the connection string is valid.

6.  **Build the Project:**
    * Go to `Build` > `Build Solution` or press `Ctrl+Shift+B`.

7.  **Run the Application:**
    * Press `F5` or click the `Start` button in Visual Studio to run the application.

