using System;
using System.ComponentModel.DataAnnotations;

public class RequestModel
{
    [Required(ErrorMessage = "Data is required.")]
    [MaxLength(100, ErrorMessage = "Data cannot be more than 50 characters.")]
    public string Data { get; set; }

    [Required(ErrorMessage = "Number is required.")]
    [Range(0, 9999999, ErrorMessage = "Number must be between 0 and 9999999.")]
    public int Number { get; set; }
}