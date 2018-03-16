using AutoMapper;
using Library.Api.Entities;
using Library.Api.Helpers;
using Library.Api.Models;
using Library.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Library.Api.Controllers
{
    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        private readonly ILibraryRepository _repository;
        private readonly IUrlHelper _urlHelper;

        public AuthorsController(ILibraryRepository repository, IUrlHelper urlHelper)
        {
            _repository = repository;
            _urlHelper = urlHelper;
        }

        [HttpGet(Name = "GetAuthors")]
        public IActionResult GetAuthors(AuthorsResourceParameters parameters)
        {
            PagedList<Author> authorsFromRepo = _repository.GetAuthors(parameters);

            string previousPageLink = authorsFromRepo.HasPrevious
                ? CreateAuthorsResourceUri(parameters, ResourceUriType.PreviousPage)
                : null;

            string nextPageLink = authorsFromRepo.HasNext
                ? CreateAuthorsResourceUri(parameters, ResourceUriType.NextPage)
                : null;

            var paginationMetadata = new
            {
                authorsFromRepo.TotalCount,
                authorsFromRepo.PageSize,
                authorsFromRepo.CurrentPage,
                authorsFromRepo.TotalPages,
                previousPageLink,
                nextPageLink
            };

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationMetadata));

            IEnumerable<AuthorDto> authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            return Ok(authors);
        }

        private string CreateAuthorsResourceUri(AuthorsResourceParameters parameters, ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return _urlHelper.Link("GetAuthors", new
                    {
                        searchQuery = parameters.SearchQuery,
                        genre = parameters.Genre,
                        pageNumber = parameters.PageNumber - 1,
                        pageSize = parameters.PageSize
                    });

                case ResourceUriType.NextPage:
                    return _urlHelper.Link("GetAuthors", new
                    {
                        searchQuery = parameters.SearchQuery,
                        genre = parameters.Genre,
                        pageNumber = parameters.PageNumber + 1,
                        pageSize = parameters.PageSize
                    });

                default:
                    return _urlHelper.Link("GetAuthors", new
                    {
                        searchQuery = parameters.SearchQuery,
                        genre = parameters.Genre,
                        pageNumber = parameters.PageNumber,
                        pageSize = parameters.PageSize
                    });
            }
        }

        [HttpGet("{id}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid id)
        {
            Author authorFromRepo = _repository.GetAuthor(id);

            if (authorFromRepo == null)
            {
                return NotFound();
            }

            AuthorDto author = Mapper.Map<AuthorDto>(authorFromRepo);

            return Ok(author);
        }

        [HttpPost]
        public IActionResult CreateAuthor([FromBody] AuthorForCreationDto author)
        {
            if (author == null)
            {
                return BadRequest();
            }

            Author authorEntity = Mapper.Map<Author>(author);

            _repository.AddAuthor(authorEntity);

            if (!_repository.Save())
            {
                throw new Exception("Creating an author failed on save.");
            }

            AuthorDto authorToReturn = Mapper.Map<AuthorDto>(authorEntity);

            return CreatedAtRoute("GetAuthor", new { id = authorToReturn.Id }, authorToReturn);
        }

        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            if (_repository.AuthorExists(id))
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }

            return NotFound();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteAuthor(Guid id)
        {
            Author authorFromRepo = _repository.GetAuthor(id);

            if (authorFromRepo == null)
            {
                return NotFound();
            }

            _repository.DeleteAuthor(authorFromRepo);

            if (!_repository.Save())
            {
                throw new Exception($"Deleting author {id} failed on save.");
            }

            return NoContent();
        }
    }
}