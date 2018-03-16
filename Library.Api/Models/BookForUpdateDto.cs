﻿using System.ComponentModel.DataAnnotations;

namespace Library.Api.Models
{
    public class BookForUpdateDto : BookForManipulationDto
    {
        [Required(ErrorMessage = "You should fill out a description.")]
        public override string Description { get; set; }
    }
}