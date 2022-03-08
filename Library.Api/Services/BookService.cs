using Dapper;
using Library.Api.Data;
using Library.Api.Models;

namespace Library.Api.Services;

public class BookService : IBookService
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public BookService(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }
    public async Task<bool> CreateAsync(Book book)
    {
        var existingBook = await GetByIsbnAsync(book.Isbn);
        if (existingBook is not null)
        {
            return false;
        }
        
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        var result = await connection.ExecuteAsync(
            @"INSERT INTO Books (Isbn, Title, Author, ShortDescription, PageCount, ReleaseDate) VALUES (@Isbn, @Title, @Author, @ShortDescription, @PageCount, @ReleaseDate)", book
        );

        return result > 0;
    }

    public async Task<bool> DeleteAsync(string isbn)
    {
        var existingBook = await GetByIsbnAsync(isbn);
        if (existingBook is null)
        {
            return false;
        }
        
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        var result = await connection.ExecuteAsync(
            @"DELETE FROM Books WHERE Isbn = @Isbn", new { Isbn = isbn }
        );

        return result > 0;
    }

    public async Task<IEnumerable<Book>> GetAllAsync()
    {
        var query = @"SELECT * FROM Books";
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();

        return await connection.QueryAsync<Book>(query);
    }

    public async Task<Book?> GetByIsbnAsync(string isbn)
    {
        var query = @"SELECT * FROM Books WHERE Isbn = @Isbn LIMIT 1";
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();

        return await connection.QuerySingleOrDefaultAsync<Book>(query, new { Isbn = isbn });
    }

    public async Task<IEnumerable<Book>> SearchByTitleAsync(string searchTerm)
    {
        var query = @"SELECT * FROM Books WHERE Title LIKE '%' || @SearchTerm || '%'";
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();

        return await connection.QueryAsync<Book>(query, new { SearchTerm = searchTerm });
    }

    public async Task<bool> UpdateAsync(Book book)
    {
        
        var existingBook = await GetByIsbnAsync(book.Isbn);
        if (existingBook is null)
        {
            return false;
        }
   
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        var result = await connection.ExecuteAsync(
            @"UPDATE Books SET Title = @Title, Author = @Author, ShortDescription = @ShortDescription, PageCount = @PageCount, ReleaseDate = @ReleaseDate WHERE Isbn = @Isbn", book
        );

        return result > 0;
    }
}