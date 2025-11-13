using System.ComponentModel;

namespace aRefactor.Domain.Exception;

public enum Response
{
    #region Default

    [Description("Thanh cong.")]
    Success = 0,
    [Description("Ban khong co quyen thuc hien hanh dong nay.")]
    Forbidden = 1,
    [Description("Ban khong co quyen truy cap.")]
    Unauthorized = 2,
    [Description("Tai nguyen khong ton tai.")]
    NotFound = 3,
    [Description("Du lieu khong hop le.")]
    ValidationError = 4,
    [Description("Da xay ra loi khong mong muon. Vui long thu lai sau.")]
    InternalServerError = 5,
    
    #endregion

    #region Dto
    
    [Description("Tên không được trống")]
    NameCannotBeEmpty = 100,
    [Description("Slug không được trống")]
    SlugCannotBeEmpty = 101,
    #endregion
}
