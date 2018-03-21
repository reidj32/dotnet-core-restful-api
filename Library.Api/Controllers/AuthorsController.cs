﻿using AutoMapper;
using Library.Api.Entities;
using Library.Api.Helpers;
using Library.Api.Models;
using Library.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace Library.Api.Controllers
{
    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        private readonly ILibraryRepository _repository;
        private readonly IUrlHelper _urlHelper;
        private readonly IPropertyMappingService _propertyMappingService;
        private readonly ITypeHelperService _typeHelperService;

        public AuthorsController(ILibraryRepository repository, IUrlHelper urlHelper,
            IPropertyMappingService propertyMappingService, ITypeHelperService typeHelperService)
        {
            _repository = repository;
            _urlHelper = urlHelper;
            _propertyMappingService = propertyMappingService;
            _typeHelperService = typeHelperService;
        }

        [HttpGet(Name = "GetAuthors")]
        public IActionResult GetAuthors(AuthorsResourceParameters parameters)
        {
            if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>(parameters.OrderBy))
            {
                return BadRequest();
            }

            if (!_typeHelperService.TypeHasProperties<AuthorDto>(parameters.Fields))
            {
                return BadRequest();
            }

            PagedList<Author> authorsFromRepo = _repository.GetAuthors(parameters);

            var paginationMetadata = new
            {
                authorsFromRepo.TotalCount,
                authorsFromRepo.PageSize,
                authorsFromRepo.CurrentPage,
                authorsFromRepo.TotalPages
            };

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationMetadata));

            IEnumerable<AuthorDto> authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            IEnumerable<LinkDto> links =
                CreateLinksForAuthors(parameters, authorsFromRepo.HasNext, authorsFromRepo.HasPrevious);

            IEnumerable<ExpandoObject> shapedAuthors = authors.ShapeData(parameters.Fields);

            IEnumerable<IDictionary<string, object>> shapedAuthorsWithLinks = shapedAuthors.Select(author =>
            {
                IDictionary<string, object> authorsAsDictionary = author;
                IEnumerable<LinkDto> authorLinks =
                    CreateLinksForAuthor((Guid)authorsAsDictionary["Id"], parameters.Fields);

                authorsAsDictionary.Add("links", authorLinks);

                return authorsAsDictionary;
            });

            return Ok(new { value = shapedAuthorsWithLinks, links });
        }

        private string CreateAuthorsResourceUri(AuthorsResourceParameters parameters, ResourceUriType type)
        {
            int pageNumber = parameters.PageNumber;

            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    pageNumber--;
                    break;

                case ResourceUriType.NextPage:
                    pageNumber++;
                    break;

                case ResourceUriType.Current:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            return _urlHelper.Link("GetAuthors", new
            {
                parameters.Fields,
                parameters.OrderBy,
                parameters.SearchQuery,
                parameters.Genre,
                pageNumber,
                parameters.PageSize
            });
        }

        [HttpGet("{id}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid id, [FromQuery] string fields)
        {
            if (!_typeHelperService.TypeHasProperties<AuthorDto>(fields))
            {
                return BadRequest();
            }

            Author authorFromRepo = _repository.GetAuthor(id);

            if (authorFromRepo == null)
            {
                return NotFound();
            }

            AuthorDto author = Mapper.Map<AuthorDto>(authorFromRepo);
            IEnumerable<LinkDto> links = CreateLinksForAuthor(id, fields);
            IDictionary<string, object> linkedResourceToReturn = author.ShapeData(fields);

            linkedResourceToReturn.Add("links", links);

            return Ok(linkedResourceToReturn);
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
            IEnumerable<LinkDto> links = CreateLinksForAuthor(authorToReturn.Id, null);
            IDictionary<string, object> linkedResourceToReturn = authorToReturn.ShapeData(null);

            linkedResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor", new { id = linkedResourceToReturn["Id"] }, linkedResourceToReturn);
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

        [HttpDelete("{id}", Name = "DeleteAuthor")]
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

        private IEnumerable<LinkDto> CreateLinksForAuthor(Guid id, string fields)
        {
            List<LinkDto> links = new List<LinkDto>
            {
                string.IsNullOrWhiteSpace(fields)
                    ? new LinkDto(_urlHelper.Link("GetAuthor", new {id}), "self", "GET")
                    : new LinkDto(_urlHelper.Link("GetAuthor", new {id, fields}), "self", "GET"),

                new LinkDto(_urlHelper.Link("DeleteAuthor", new {id}), "delete_author", "DELETE"),

                new LinkDto(_urlHelper.Link("CreateBookForAuthor", new {authorId = id}),
                    "create_book_for_author", "POST"),

                new LinkDto(_urlHelper.Link("GetBooksForAuthor", new {authorId = id}), "books", "GET")
            };

            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForAuthors(AuthorsResourceParameters parameters, bool hasNext,
            bool hasPrevious)
        {
            List<LinkDto> links = new List<LinkDto>
            {
                new LinkDto(CreateAuthorsResourceUri(parameters, ResourceUriType.Current), "self", "GET")
            };

            if (hasNext)
            {
                links.Add(
                    new LinkDto(CreateAuthorsResourceUri(parameters, ResourceUriType.NextPage), "nextPage", "GET"));
            }

            if (hasPrevious)
            {
                links.Add(new LinkDto(CreateAuthorsResourceUri(parameters, ResourceUriType.PreviousPage),
                    "previousPage", "GET"));
            }

            return links;
        }
    }
}