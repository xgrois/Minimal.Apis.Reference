using FluentValidation;
using FluentValidation.Results;
using Library.Api.Auth;
using Library.Api.Models;
using Library.Api.Services;
using System.Net;

namespace Library.Api.EndPoints;
public static class LibraryEndPoints
{
    public static void AddLibraryEndPoints(this IServiceCollection services)
    {
        // Add all services related to the Library
        services.AddSingleton<IBookService, BookService>();
    }

    public static void UseLibraryEndPoints(this IEndpointRouteBuilder app)
    {
        // ../books
        // ../books?searchTerm=
        // ../books?searchTerm=ergo
        app.MapGet("books", GetAllBooksWithOptionalTitleSearchAsync)
            .WithTags("Books")
            .WithName("GetBooks")
            .Produces<IEnumerable<Book>>(200);

        app.MapGet("books/{isbn}", GetBookByIsbnAsync)
            .WithTags("Books")
            .WithName("GetBook")
            .Produces<Book>(200)
            .Produces(404);


        app.MapPost("books",
            // If want to test authorization
            //[Authorize(AuthenticationSchemes = ApiKeySchemeConstants.SchemeName)] 
            CreateBookAsync)
            .WithTags("Books")
            .WithName("CreateBook")
            .Accepts<Book>("application/json")
            .Produces<Book>(201)
            .Produces<IEnumerable<ValidationFailure>>(400);

        app.MapPut("books/{isbn}", UpdateBookAsync)
            .WithTags("Books")
            .WithName("UpdateBook")
            .Accepts<Book>("application/json")
            .Produces<Book>(200)
            .Produces(404)
            .Produces<IEnumerable<ValidationFailure>>(400);

        app.MapDelete("books/{isbn}", DeleteBookAsync)
            .WithTags("Books")
            .WithName("DeleteBook")
            .Produces(204)
            .Produces(404);
    }

    private static async Task<IResult> DeleteBookAsync(string isbn, IBookService bookService)
    {

        var deleted = await bookService.DeleteAsync(isbn);

        return deleted ? Results.NoContent() : Results.NotFound();

    }

    private static async Task<IResult> UpdateBookAsync(string isbn, Book book, IBookService bookService, IValidator<Book> validator)
    {

        book.Isbn = isbn; // ISBN in book is irrelevant. Should always be part of the URL

        var validationResult = await validator.ValidateAsync(book);
        if (!validationResult.IsValid)
            return Results.BadRequest(validationResult.Errors);

        var updated = await bookService.UpdateAsync(book);

        return updated ? Results.Ok(book) : Results.NotFound();

    }

    private static async Task<IResult> CreateBookAsync(Book book, IBookService bookService, IValidator<Book> validator)
    {

        var validationResult = await validator.ValidateAsync(book);
        if (!validationResult.IsValid)
            return Results.BadRequest(validationResult.Errors);

        var created = await bookService.CreateAsync(book);
        if (!created)
        {
            return Results.BadRequest(new List<ValidationFailure>
                {
            new ("Isbn", $"A book with ISBN-13 {book.Isbn} already exists")
                });
        }

        //return Results.CreatedAtRoute("CreateBook", new { isbn = book.Isbn }, book); // Naming endpoints helps in code maintainability...
        return Results.Created($"/books/{book.Isbn}", book);

    }

    private static async Task<IResult> GetBookByIsbnAsync(string isbn, IBookService bookService)
    {

        var book = await bookService.GetByIsbnAsync(isbn);

        return book is not null ? Results.Ok(book) : Results.NotFound();

    }

    private static async Task<IResult> GetAllBooksWithOptionalTitleSearchAsync(IBookService bookService, string? searchTerm)
    {

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var booksMatched = await bookService.SearchByTitleAsync(searchTerm!);
            return Results.Ok(booksMatched);
        }
        var books = await bookService.GetAllAsync();
        return Results.Ok(books);

    }
}

