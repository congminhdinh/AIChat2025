using System.ComponentModel.DataAnnotations;

public enum ChatFeedbackCategory
{
    Initialized = 0,
    // --- Nhóm Độ chính xác (Accuracy) ---
    [Display(Name = "Sai kiến thức pháp luật")]
    WrongKnowledge = 1,

    [Display(Name = "Thông tin hết hiệu lực")]
    OutdatedInfo = 2,

    [Display(Name = "Câu trả lời mâu thuẫn")]
    ConflictingInfo = 3,

    [Display(Name = "Hiểu sai ý người dùng")]
    Misunderstood = 4,

    // --- Nhóm Trích dẫn & RAG (Retrieval) ---
    [Display(Name = "Trích dẫn sai nguồn")]
    WrongCitation = 5,

    [Display(Name = "Thiếu căn cứ pháp lý")]
    MissingLegalBasis = 6,

    [Display(Name = "Nguồn không liên quan")]
    IrrelevantSource = 7,

    // --- Nhóm Chất lượng (Quality) ---
    [Display(Name = "Trả lời quá lan man")]
    TooVerbose = 8,

    [Display(Name = "Trả lời quá sơ sài")]
    TooBrief = 9,

    // --- Nhóm Kỹ thuật (Technical) ---
    [Display(Name = "Phản hồi quá chậm")]
    SlowResponse = 10,

    [Display(Name = "Lỗi hiển thị/Định dạng")]
    FormattingError = 11,

    [Display(Name = "Khác")]
    Other = 99
}