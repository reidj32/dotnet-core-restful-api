using AutoMapper;
using Library.Api.Entities;
using Library.Api.Helpers;
using Library.Api.Models;
using Library.Api.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Library.Api.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private readonly ILibraryRepository _repository;
        private readonly IUrlHelper _urlHelper;
        private readonly ILogger<BooksController> _logger;

        public BooksController(ILibraryRepository repository, IUrlHelper urlHelper, ILogger<BooksController> logger)
        {
            _repository = repository;
            _urlHelper = urlHelper;
            _logger = logger;
        }

        [HttpGet(Name = "GetBooksForAuthor")]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            IEnumerable<Book> booksForAuthorFromRepo = _repository.GetBooksForAuthor(authorId);
            IEnumerable<BookDto> booksForAuthor = Mapper.Map<IEnumerable<BookDto>>(booksForAuthorFromRepo);

            booksForAuthor = booksForAuthor.Select(book =>
            {
                book = CreateLinksForBook(book);
                return book;
            });

            LinkedCollectionResourceWrapperDto<BookDto> wrapper =
                new LinkedCollectionResourceWrapperDto<BookDto>(booksForAuthor);

            return Ok(CreateLinksForBooks(wrapper));
        }

        [HttpGet("{id}", Name = "GetBookForAuthor")]
        public IActionResult GetBookForAuthor(Guid authorId, Guid id)
        {
            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            Book bookForAuthorFromRepo = _repository.GetBookForAuthor(authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                return NotFound();
            }

            BookDto bookForAuthor = Mapper.Map<BookDto>(bookForAuthorFromRepo);

            return Ok(CreateLinksForBook(bookForAuthor));
        }

        [HttpPost(Name = "CreateBookForAuthor")]
        public IActionResult CreateBookForAuthor(Guid authorId, [FromBody] BookForCreationDto book)
        {
            if (book == null)
            {
                return BadRequest();
            }

            if (book.Description == book.Title)
            {
                ModelState.AddModelError(nameof(BookForCreationDto),
                    "The provided description should be different from the title.");
            }

            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }

            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            Book bookEntity = Mapper.Map<Book>(book);

            _repository.AddBookForAuthor(authorId, bookEntity);

            if (!_repository.Save())
            {
                throw new Exception($"Creating a book for author {authorId} failed on save.");
            }

            BookDto bookToReturn = Mapper.Map<BookDto>(bookEntity);

            return CreatedAtRoute("GetBookForAuthor", new { authorId, id = bookToReturn.Id },
                CreateLinksForBook(bookToReturn));
        }

        [HttpDelete("{id}", Name = "DeleteBookForAuthor")]
        public IActionResult DeleteBookForAuthor(Guid authorId, Guid id)
        {
            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            Book bookForAuthorFromRepo = _repository.GetBookForAuthor(authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                return NotFound();
            }

            _repository.DeleteBook(bookForAuthorFromRepo);

            if (!_repository.Save())
            {
                throw new Exception($"Deleting book {id} for author {authorId} failed on save.");
            }

            _logger.LogInformation(100, $"Book {id} for author {authorId} was deleted.");

            return NoContent();
        }

        [HttpPut("{id}", Name = "UpdateBookForAuthor")]
        public IActionResult UpdateBookForAuthor(Guid authorId, Guid id, [FromBody] BookForUpdateDto book)
        {
            if (book == null)
            {
                return BadRequest();
            }

            if (book.Description == book.Title)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto),
                    "The provided description should be different from the title.");
            }

            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }

            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            Book bookForAuthorFromRepo = _repository.GetBookForAuthor(authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                // Upserting logic

                Book bookToAdd = Mapper.Map<Book>(book);
                bookToAdd.Id = id;

                _repository.AddBookForAuthor(authorId, bookToAdd);

                if (!_repository.Save())
                {
                    throw new Exception($"Upserting book {id} for author {authorId} failed on save.");
                }

                BookDto bookToReturn = Mapper.Map<BookDto>(bookToAdd);

                return CreatedAtRoute("GetBookForAuthor", new { authorId, id = bookToReturn.Id }, bookToReturn);
            }

            Mapper.Map(book, bookForAuthorFromRepo);

            _repository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!_repository.Save())
            {
                throw new Exception($"Updating book {id} for author {authorId} failed on save.");
            }

            return NoContent();
        }

        [HttpPatch("{id}", Name = "PartiallyUpdateBookForAuthor")]
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorId, Guid id,
            [FromBody] JsonPatchDocument<BookForUpdateDto> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest();
            }

            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            Book bookForAuthorFromRepo = _repository.GetBookForAuthor(authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                // Upserting logic

                BookForUpdateDto bookDto = new BookForUpdateDto();
                patchDoc.ApplyTo(bookDto, ModelState);

                if (bookDto.Description == bookDto.Title)
                {
                    ModelState.AddModelError(nameof(BookForUpdateDto),
                        "The provided description should be different from the title.");
                }

                TryValidateModel(bookDto);

                if (!ModelState.IsValid)
                {
                    return new UnprocessableEntityObjectResult(ModelState);
                }

                Book bookToAdd = Mapper.Map<Book>(bookDto);
                bookToAdd.Id = id;

                _repository.AddBookForAuthor(authorId, bookToAdd);

                if (!_repository.Save())
                {
                    throw new Exception($"Upserting book {id} for author {authorId} failed on save.");
                }

                BookDto bookToReturn = Mapper.Map<BookDto>(bookToAdd);

                return CreatedAtRoute("GetBookForAuthor", new { authorId, id = bookToReturn.Id }, bookToReturn);
            }

            BookForUpdateDto bookToPatch = Mapper.Map<BookForUpdateDto>(bookForAuthorFromRepo);

            patchDoc.ApplyTo(bookToPatch, ModelState);

            if (bookToPatch.Description == bookToPatch.Title)
            {
                ModelState.AddModelError(nameof(BookForUpdateDto),
                    "The provided description should be different from the title.");
            }

            TryValidateModel(bookToPatch);

            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }

            Mapper.Map(bookToPatch, bookForAuthorFromRepo);

            _repository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!_repository.Save())
            {
                throw new Exception($"Patching book {id} for author {authorId} failed on save.");
            }

            return NoContent();
        }

        private BookDto CreateLinksForBook(BookDto book)
        {
            book.Links.Add(
                new LinkDto(_urlHelper.Link("GetBookForAuthor", new { id = book.Id }), "self", "GET"));

            book.Links.Add(
                new LinkDto(_urlHelper.Link("DeleteBookForAuthor", new { id = book.Id }), "delete_book", "DELETE"));

            book.Links.Add(
                new LinkDto(_urlHelper.Link("UpdateBookForAuthor", new { id = book.Id }), "update_book", "PUT"));

            book.Links.Add(
                new LinkDto(_urlHelper.Link("PartiallyUpdateBookForAuthor", new { id = book.Id }),
                    "partially_update_book", "PATCH"));

            return book;
        }

        private LinkedCollectionResourceWrapperDto<BookDto> CreateLinksForBooks(
            LinkedCollectionResourceWrapperDto<BookDto> booksWrapper)
        {
            booksWrapper.Links.Add(
                new LinkDto(_urlHelper.Link("GetBooksForAuthor", new { }), "self", "GET"));

            return booksWrapper;
        }
    }
}