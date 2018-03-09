﻿using AutoMapper;
using Library.Api.Entities;
using Library.Api.Models;
using Library.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Library.Api.Controllers
{
    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        private readonly ILibraryRepository _repository;

        public AuthorsController(ILibraryRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public IActionResult GetAuthors()
        {
            IEnumerable<Author> authorsFromRepo = _repository.GetAuthors();
            IEnumerable<AuthorDto> authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            return Ok(authors);
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