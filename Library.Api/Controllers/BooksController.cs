using AutoMapper;
using Library.Api.Entities;
using Library.Api.Models;
using Library.Api.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Library.Api.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private readonly ILibraryRepository _repository;

        public BooksController(ILibraryRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            if (!_repository.AuthorExists(authorId))
            {
                return NotFound();
            }

            IEnumerable<Book> booksForAuthorFromRepo = _repository.GetBooksForAuthor(authorId);
            IEnumerable<BookDto> booksForAuthor = Mapper.Map<IEnumerable<BookDto>>(booksForAuthorFromRepo);

            return Ok(booksForAuthor);
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

            return Ok(bookForAuthor);
        }

        [HttpPost]
        public IActionResult CreateBookForAuthor(Guid authorId, [FromBody] BookForCreationDto book)
        {
            if (book == null)
            {
                return BadRequest();
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

            return CreatedAtRoute("GetBookForAuthor", new { authorId, id = bookToReturn.Id }, bookToReturn);
        }

        [HttpDelete("{id}")]
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

            return NoContent();
        }

        [HttpPut("{id}")]
        public IActionResult UpdateBookForAuthor(Guid authorId, Guid id, [FromBody] BookForUpdateDto book)
        {
            if (book == null)
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

        [HttpPatch("{id}")]
        public IActionResult PartiallyUpdateBookForAuthor(Guid authorId, Guid id, [FromBody] JsonPatchDocument<BookForUpdateDto> patchDoc)
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
                patchDoc.ApplyTo(bookDto);

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

            patchDoc.ApplyTo(bookToPatch);

            // TODO: add validation

            Mapper.Map(bookToPatch, bookForAuthorFromRepo);

            _repository.UpdateBookForAuthor(bookForAuthorFromRepo);

            if (!_repository.Save())
            {
                throw new Exception($"Patching book {id} for author {authorId} failed on save.");
            }

            return NoContent();
        }
    }
}