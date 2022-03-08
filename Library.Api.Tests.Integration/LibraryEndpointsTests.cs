using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Library.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Library.Api.Tests.Integration;

public class LibraryEndpointsTests : IClassFixture<WebApplicationFactory<IApiMarker>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<IApiMarker> _factory;
    private readonly List<string> _createdIsbns = new();

    public LibraryEndpointsTests(WebApplicationFactory<IApiMarker> factory)
    {
        _factory = factory;
    }


    [Fact]
    public async Task CreateBook_CreatesBook_WhenDataIsCorrect()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateFakeBook();

        // Act
        var result = await httpClient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);
        var createdBook = await result.Content.ReadFromJsonAsync<Book>();

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        createdBook.Should().BeEquivalentTo(book);
        result.Headers.Location.Should().Be($"/books/{book.Isbn}");
    }

    [Fact]
    public async Task CreateBook_Fails_WhenIsbnIsInvalid()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateFakeBook();
        book.Isbn = "abcd1234";

        // Act
        var result = await httpClient.PostAsJsonAsync("/books", book);
        var validationErrors = await result.Content.ReadFromJsonAsync<IEnumerable<ValidationError>>();
        var error = validationErrors!.Single();

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.PropertyName.Should().Be("Isbn");
        error.ErrorMessage.Should().Be("Value was not a valid ISBN-13");
    }

    [Fact]
    public async Task CreateBook_Fails_WhenBookExists()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateFakeBook();

        // Act
        await httpClient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);
        var result = await httpClient.PostAsJsonAsync("/books", book);
        var validationErrors = await result.Content.ReadFromJsonAsync<IEnumerable<ValidationError>>();
        var error = validationErrors!.Single();

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.PropertyName.Should().Be("Isbn");
        error.ErrorMessage.Should().Be($"A book with ISBN-13 {book.Isbn} already exists");
    }

    [Fact]
    public async Task GetBook_ReturnsBook_WhenBookExists()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateFakeBook();
        await httpClient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);

        // Act
        var result = await httpClient.GetAsync($"/books/{book.Isbn}");
        var existingBook = await result.Content.ReadFromJsonAsync<Book>();

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        existingBook.Should().BeEquivalentTo(book);
    }

    [Fact]
    public async Task GetBook_ReturnsNotFound_WhenBookDoesNotExists()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var isbn = GenerateFakeIsbn();

        // Act
        var result = await httpClient.GetAsync($"/books/{isbn}");

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllBooks_ReturnsAllBooks_WhenBooksExist()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var books = new List<Book>();
        for (int i = 0; i < 3; i++)
        {
            var book = GenerateFakeBook();
            books.Add(book);
            await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);
        }

        // Act
        var result = await httpClient.GetAsync($"/books");
        var returnedBooks = await result.Content.ReadFromJsonAsync<List<Book>>();

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        returnedBooks.Should().BeEquivalentTo(books);
    }

    [Fact]
    public async Task GetAllBooks_ReturnsEmptyList_WhenNoBooksExist()
    {
        // Arrange
        var httpClient = _factory.CreateClient();

        // Act
        var result = await httpClient.GetAsync($"/books");
        var returnedBooks = await result.Content.ReadFromJsonAsync<List<Book>>();

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        //returnedBooks.Should().BeEquivalentTo(new List<Book>());
        returnedBooks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllBooks_ReturnsBooks_WhenTitleMatches()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var books = new List<Book>();
        for (int i = 0; i < 3; i++)
        {
            var book = GenerateFakeBook();
            if (i % 2 == 0)
            {
                book.Title = book.Title + "extra";
                books.Add(book);
            }
            await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);
        }

        // Act
        var result = await httpClient.GetAsync($"/books?searchTerm=ext");
        var returnedBooks = await result.Content.ReadFromJsonAsync<List<Book>>();

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        returnedBooks.Should().BeEquivalentTo(books);
    }

    [Fact]
    public async Task UpdateBook_UpdatesBook_WhenDataIsCorrect()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateFakeBook();
        await httpClient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);

        // Act
        book.PageCount = 111;
        var result = await httpClient.PutAsJsonAsync($"/books/{book.Isbn}", book);
        var updatedBook = await result.Content.ReadFromJsonAsync<Book>();

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedBook.Should().BeEquivalentTo(book);
    }

    [Fact]
    public async Task UpdateBook_ReturnsNotFound_WhenBookDoesNotExists()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateFakeBook();
        await httpClient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);

        // Act
        book.PageCount = 111;
        book.Isbn = "123-1231231230";
        var result = await httpClient.PutAsJsonAsync($"/books/{book.Isbn}", book);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBook_DoesNotUpdateBook_WhenDataIsIncorrect()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateFakeBook();
        await httpClient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);

        // Act
        book.Title = String.Empty; // cannot be empty
        var result = await httpClient.PutAsJsonAsync($"/books/{book.Isbn}", book);
        var validationErrors = await result.Content.ReadFromJsonAsync<IEnumerable<ValidationError>>();
        var error = validationErrors!.Single();

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.PropertyName.Should().Be("Title");
        error.ErrorMessage.Should().Be("'Title' must not be empty.");
    }

    [Fact]
    public async Task DeleteBook_ReturnsNotFound_WhenBookDoesNotExists()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var isbn = GenerateFakeIsbn();

        // Act
        var result = await httpClient.DeleteAsync($"/books/{isbn}");

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteBook_DeletesBook_WhenBookExists()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var book = GenerateFakeBook();
        await httpClient.PostAsJsonAsync("/books", book);
        _createdIsbns.Add(book.Isbn);

        // Act
        var result = await httpClient.DeleteAsync($"/books/{book.Isbn}");

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private Book GenerateFakeBook(string title = "Learning C# by Samples")
    {
        return new Book
        {
            Isbn = GenerateFakeIsbn(),
            Title = title,
            Author = "Federico",
            PageCount = 444,
            ShortDescription = "Find your own path to C# with the greatests and latests samples!",
            ReleaseDate = new DateTime(2023, 1, 1)
        };
    }

    private string GenerateFakeIsbn()
    {
        return $"{Random.Shared.Next(100, 999)}-" + $"{Random.Shared.Next(1000000000, 2000000000)}";
    }

    // We don't care about InitializeAsync
    public Task InitializeAsync() => Task.CompletedTask;

    // Here we delete all created books during tests in the database
    // This is called after each Test function, not after all tests
    public async Task DisposeAsync()
    {
        var httpClient = _factory.CreateClient();
        foreach (var isbn in _createdIsbns)
        {
            await httpClient.DeleteAsync($"/books/{isbn}");
        }
    }
}