using AutoMapper;
using Library.Api.Entities;
using Library.Api.Helpers;
using Library.Api.Models;
using Library.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Library.Api.Controllers
{
    [Route("api/authorcollections")]
    public class AuthorCollectionsController : Controller
    {
        private readonly ILibraryRepository _repository;

        public AuthorCollectionsController(ILibraryRepository repository)
        {
            _repository = repository;
        }

        [HttpPost]
        public IActionResult CreateAuthorCollection([FromBody] IEnumerable<AuthorForCreationDto> authorCollection)
        {
            if (authorCollection == null)
            {
                return BadRequest();
            }

            IEnumerable<Author> authorEntities = Mapper.Map<IEnumerable<Author>>(authorCollection);

            foreach (Author author in authorEntities)
            {
                _repository.AddAuthor(author);
            }

            if (!_repository.Save())
            {
                throw new Exception("Creating an author collection failed on save.");
            }

            IEnumerable<AuthorDto> authorCollectionToReturn = Mapper.Map<IEnumerable<AuthorDto>>(authorEntities);
            string idsAsString = string.Join(",", authorCollectionToReturn.Select(a => a.Id));

            return CreatedAtRoute("GetAuthorCollection", new { ids = idsAsString }, authorCollectionToReturn);
        }

        [HttpGet("({ids})", Name = "GetAuthorCollection")]
        public IActionResult GetAuthorCollection([ModelBinder(typeof(ArrayModelBinder))] IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                return BadRequest();
            }

            IEnumerable<Guid> authorIds = ids.ToList();
            IEnumerable<Author> authorEntities = _repository.GetAuthors(authorIds);

            if (authorIds.Count() != authorEntities.Count())
            {
                return NotFound();
            }

            IEnumerable<AuthorDto> authorsToReturn = Mapper.Map<IEnumerable<AuthorDto>>(authorEntities);

            return Ok(authorsToReturn);
        }
    }
}