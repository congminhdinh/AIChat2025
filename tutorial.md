# LaTeX Graduation Thesis Template - Tutorial Guide

## Table of Contents
1. [Project Overview](#project-overview)
2. [Project Structure](#project-structure)
3. [Prerequisites](#prerequisites)
4. [How to Build/Compile](#how-to-buildcompile)
5. [Getting Started - Customization](#getting-started---customization)
6. [Custom Commands and Features](#custom-commands-and-features)
7. [Working with Chapters](#working-with-chapters)
8. [Working with References](#working-with-references)
9. [Working with Images and Tables](#working-with-images-and-tables)
10. [Working with Acronyms and Glossary](#working-with-acronyms-and-glossary)
11. [Formatting Conventions](#formatting-conventions)
12. [Important Notes and Guidelines](#important-notes-and-guidelines)

---

## Project Overview

This is a **Vietnamese Graduation Thesis (Đồ Án Tốt Nghiệp - ĐATN)** template specifically designed for students at **Hanoi University of Science and Technology (HUST)**, School of Information and Communication Technology (SoICT).

The template follows the **ISO 7144:1986** standard for technical reports and is pre-configured with:
- Vietnamese language support
- IEEE citation style
- Proper page formatting and margins
- Automated table of contents, list of figures, and list of tables
- Glossary and acronym management
- Appendices support

---

## Project Structure

```
SOICT_DATN_Application_VIE_Template/
│
├── DoAn.tex                          # MAIN FILE - Compile this file
├── Bia.tex                           # Front cover page
├── Bia_lot.tex                       # Inner cover page
├── Tu_viet_tat.tex                   # Acronyms and glossary definitions
├── Danh_sach_tai_lieu_tham_khao.bib  # Bibliography/references file
├── lstlisting                        # Code listing configuration (referenced but may need creation)
│
├── Chuong/                           # Chapters directory
│   ├── 0_2_Loi_cam_on.tex           # Acknowledgments
│   ├── 0_3_Tom_tat_noi_dung.tex     # Vietnamese abstract
│   ├── 0_4_Tom_tat_noi_dung_English.tex  # English abstract
│   ├── 0_5_Danh_muc_viet_tat.tex    # List of abbreviations (optional)
│   ├── 0_6_Thuat_ngu.tex            # Terminology (optional)
│   ├── 1_Gioi_thieu.tex             # Chapter 1: Introduction
│   ├── 2_Khao_sat.tex               # Chapter 2: Survey and Analysis
│   ├── 3_Cong_nghe.tex              # Chapter 3: Technologies Used
│   ├── 4_Ket_qua_thuc_nghiem.tex    # Chapter 4: Design and Implementation
│   ├── 5_Giai_phap_dong_gop.tex     # Chapter 5: Solutions and Contributions
│   ├── 6_Ket_luan.tex               # Chapter 6: Conclusion
│   ├── 7_Luu_y_tai_lieu_tham_khao.tex  # Notes on references
│   ├── Phu_luc_A.tex                # Appendix A: Thesis writing guidelines
│   └── Phu_luc_B.tex                # Appendix B: Use case specifications
│
└── Hinhve/                          # Images directory
    ├── Bia.PNG
    ├── GayBia.png
    ├── IoT.png
    ├── Picture1.png
    ├── Picture2.png
    └── Picture3.png
```

---

## Prerequisites

### Required LaTeX Packages

The template uses the following packages (already included in `DoAn.tex`):

#### Document Class and Font
- `report` - Base document class
- `scrextend` - Extended KOMA-Script functionality
- `times` - Times New Roman font
- Font size: 13pt

#### Language and Encoding
- `vietnam` with `utf8` - Vietnamese language support

#### Page Layout
- `geometry` - Page margins (top: 2cm, bottom: 2cm, left: 3.5cm, right: 2.5cm)
- `fancyhdr` - Custom headers and footers
- `pdflscape` - Landscape pages support

#### Graphics and Figures
- `graphicx` - Image insertion
- `float` - Better float positioning
- `caption` - Enhanced caption formatting
- `subcaption` - Subfigures support

#### Tables
- `array` - Advanced table formatting
- `multirow` - Merge table cells

#### Mathematics
- `amsmath` - Advanced math support
- `amssymb` - Math symbols
- `amsthm` - Theorem environments
- `latexsym` - Additional LaTeX symbols
- `amsbsy` - Bold math symbols

#### Lists and Formatting
- `enumitem` - Customizable lists
- `indentfirst` - Indent first paragraph
- `setspace` - Line spacing control
- `parskip` - Paragraph spacing

#### Structure and Sectioning
- `titlesec` - Section formatting
- `titletoc` - Table of contents formatting
- `chngcntr` - Counter management
- `subfiles` - Split document into multiple files

#### Code Listings
- `listings` - Source code formatting
- `algorithm2e` - Algorithm typesetting (with `ruled` and `vlined` options)

#### References and Citations
- `biblatex` - Bibliography management (backend: bibtex, style: ieee)
- `hyperref` - Hyperlinks and PDF metadata
- `hypcap` - Correct hyperlink references
- `xurl` - URL line breaking

#### Glossary and Acronyms
- `glossaries` - Glossary and acronym management
  - Options: `nonumberlist`, `nopostdot`, `nogroupskip`, `acronym`
- `glossary-superragged` - Glossary styling

#### Other Utilities
- `fancybox` - Decorative boxes
- `appendix` - Appendix formatting
- `capt-of` - Captions for non-float elements
- `afterpage` - Execute commands after page break
- `tocbasic` - Table of contents utilities
- `blindtext` - Dummy text generation

### LaTeX Distribution
You need one of the following:
- **TeX Live** (Linux/Windows/Mac) - Recommended
- **MiKTeX** (Windows)
- **MacTeX** (macOS)

Or use an online platform:
- **Overleaf** (Recommended for beginners)

---

## How to Build/Compile

### Method 1: Command Line (Recommended)

The template uses `biblatex` with `bibtex` backend. Compile in this order:

```bash
pdflatex DoAn.tex
bibtex DoAn
pdflatex DoAn.tex
pdflatex DoAn.tex
```

**Why multiple compilations?**
- First `pdflatex`: Generates auxiliary files
- `bibtex`: Processes bibliography
- Second `pdflatex`: Incorporates bibliography references
- Third `pdflatex`: Finalizes cross-references and page numbers

### Method 2: Using LaTeX Editor

Most LaTeX editors (TeXstudio, TeXmaker, etc.) have a "Build" button that handles the compilation sequence automatically.

1. Open `DoAn.tex` in your editor
2. Set the compiler to **pdfLaTeX**
3. Click "Build" or press F5/F6 (depends on editor)
4. The editor should automatically run bibtex when needed

### Method 3: Overleaf

1. Upload the entire project folder to Overleaf
2. Set the main document to `DoAn.tex`
3. Click "Recompile"

---

## Getting Started - Customization

### Step 1: Edit Cover Page Information

Open `DoAn.tex` and modify lines 77-78:

```latex
\def \TITLE{ĐỒ ÁN TỐT NGHIỆP}
\def \AUTHOR{NGUYỄN VĂN ABC}
```

Then edit `Bia.tex` (front cover) and `Bia_lot.tex` (inner cover):

```latex
% In Bia.tex / Bia_lot.tex (around lines 16-22)
{\textbf{\Large{Thiết kế xây dựng công nghệ thực tế ảo và ứng dụng}}\\[1cm]
{\textbf{\large{NGUYỄN VĂN A}}}\\
{\large{nguyenvanabc@sis.hust.edu.vn}}\\[0.5cm]
{\textbf{\large{Chương trình đào tạo: Công nghệ thông tin Việt-Nhật}}}\\
```

Update:
- Thesis title
- Your name
- Your email
- Training program (see Appendix A section 1 for correct program names)
- Advisor information (line 32)
- Submission date (line 40)

### Step 2: Write Content in Chapter Files

**DO NOT** create new files. Write directly in the existing chapter files in the `Chuong/` directory:

- `1_Gioi_thieu.tex` - Introduction
- `2_Khao_sat.tex` - Literature review
- `3_Cong_nghe.tex` - Technologies
- `4_Ket_qua_thuc_nghiem.tex` - Design and implementation
- `5_Giai_phap_dong_gop.tex` - Solutions and contributions
- `6_Ket_luan.tex` - Conclusion

### Step 3: Update References

Edit `Danh_sach_tai_lieu_tham_khao.bib` and add your references in BibTeX format:

```bibtex
@article{your_reference_key,
  title={Paper Title},
  author={Author Name},
  journal={Journal Name},
  year={2023},
  publisher={Publisher}
}
```

### Step 4: Add Images

Place all images in the `Hinhve/` directory. The template is configured to look for images in this folder (see line 144 in DoAn.tex):

```latex
\graphicspath{{figures/}{../figures/}}
```

Note: You may need to update this to:
```latex
\graphicspath{{Hinhve/}{../Hinhve/}}
```

---

## Custom Commands and Features

### 1. Custom Command: `\underwrite`

Defined in both `Bia.tex` and `Bia_lot.tex` (line 2-4):

```latex
\newcommand{\underwrite}[3][]{%
  \genfrac{}{}{#1}{}{\textstyle #2}{\scriptstyle #3}
}
```

**Usage:** Creates a fraction-like structure for signatures
```latex
\underwrite[thickness]{numerator}{denominator}
```

**Example:**
```latex
\underwrite{Chữ ký}{GVHD}
```

### 2. Document Title and Author

These macros are defined in `DoAn.tex` (lines 77-78):

```latex
\def \TITLE{ĐỒ ÁN TỐT NGHIỆP}
\def \AUTHOR{NGUYỄN VĂN ABC}
```

Used in PDF metadata (lines 139-140):
```latex
\hypersetup{pdftitle={\TITLE},
    pdfauthor={\AUTHOR}}
```

### 3. Chapter and Section Formatting

The template customizes chapter and section headings:

**Chapter format** (lines 81-91):
```latex
\titleformat{\chapter}[hang]{\centering\bfseries}{CHƯƠNG \thechapter.\ }{0pt}{}[]
\titlespacing*{\chapter}{0pt}{-20pt}{20pt}
```

**Section format** (lines 93-101):
```latex
\titleformat{\section}[hang]{\bfseries}{\thechapter.\arabic{section}\ \ \ \ }{0pt}{}[]
```

**Subsection format** (lines 103-111):
```latex
\titleformat{\subsection}[hang]{\bfseries}{\thechapter.\arabic{section}.\arabic{subsection}\ \ \ \ }{0pt}{}[]
```

**Subsubsection format** (lines 113-122):
- Uses alphabetic numbering (a, b, c, ...)

### 4. Page Numbering Styles

The template uses different page numbering for different sections:

- **Front matter** (acknowledgments, abstracts): `\pagenumbering{gobble}` - No page numbers
- **Table of contents, lists**: `\pagenumbering{roman}` - Roman numerals (i, ii, iii, ...)
- **Main content**: `\pagenumbering{arabic}` - Arabic numerals (1, 2, 3, ...)

### 5. Example Environment

Defined at line 155:
```latex
\newtheorem{example}{Ví dụ}[chapter]
```

**Usage:**
```latex
\begin{example}
This is an example.
\end{example}
```

---

## Working with Chapters

### Chapter File Structure

Each chapter file should follow this structure:

```latex
\documentclass[../DoAn.tex]{subfiles}
\begin{document}

% Chapter content here
\section{Section Title}
Content...

\subsection{Subsection Title}
Content...

\subsubsection{Subsubsection Title}
Content...

\end{document}
```

### Adding Chapters to Main Document

In `DoAn.tex`, chapters are included using `\subfile{}`:

```latex
\chapter{CHAPTER TITLE}
\label{chapter:label_name}
\subfile{Chuong/filename}
```

**Example:**
```latex
\chapter{GIỚI THIỆU ĐỀ TÀI}
\label{chapter:Introduction}
\subfile{Chuong/1_Gioi_thieu}
```

### Chapter Guidelines (from template comments)

Each chapter should include:

1. **Tổng quan (Overview)** - Introduction to the chapter
   - Link to previous chapter
   - Explain why this chapter is necessary
   - Preview what will be covered

2. **Main content sections**

3. **Kết chương (Chapter conclusion)**
   - Summarize key points
   - Don't repeat the overview verbatim
   - Provide transition to next chapter

**Chapter 1 Specific Structure** (from 1_Gioi_thieu.tex):
- Length: 3-6 pages
- Sections:
  - 1.1 Đặt vấn đề (Problem statement)
  - 1.2 Mục tiêu và phạm vi đề tài (Objectives and scope)
  - 1.3 Định hướng giải pháp (Solution approach)
  - 1.4 Bố cục đồ án (Thesis structure)

---

## Working with References

### Adding References

Edit `Danh_sach_tai_liu_tham_khao.bib`:

**Article:**
```bibtex
@article{hovy1993automated,
  title={Automated discourse generation},
  author={Hovy, Eduard H},
  journal={Artificial intelligence},
  volume={63},
  number={1-2},
  pages={341--385},
  year={1993},
  publisher={Elsevier}
}
```

**Book:**
```bibtex
@book{peterson2007computer,
  title={Computer networks: a systems approach},
  author={Peterson, Larry L and Davie, Bruce S},
  year={2007},
  publisher={Elsevier}
}
```

**Conference Paper:**
```bibtex
@inproceedings{poesio2001discourse,
  title={Discourse structure and anaphoric accessibility},
  author={Poesio, Massimo},
  booktitle={ESSLLI workshop},
  pages={129--143},
  year={2001}
}
```

**Website:**
```bibtex
@misc{BernersTim,
  author = {Berners-Lee, Tim},
  title = {Hypertext Transfer Protocol},
  url = {http://example.com},
  urldate = {2010-09-30}
}
```

### Citing References

Use `\cite{reference_key}`:

```latex
According to research \cite{hovy1993automated}, the method is effective.
```

Multiple citations:
```latex
Several studies \cite{hovy1993automated, peterson2007computer} have shown...
```

### Bibliography Customization

The template uses IEEE style (line 53):
```latex
\usepackage[backend=bibtex,style=ieee]{biblatex}
```

Bibliography title is customized (line 311):
```latex
\renewcommand\bibname{TÀI LIỆU THAM KHẢO}
```

---

## Working with Images and Tables

### Inserting Images

**Basic syntax:**
```latex
\begin{figure}[H]
    \centering
    \includegraphics[width=0.75\linewidth]{Hinhve/imagename.png}
    \caption{Image caption here}
    \label{fig:image_label}
\end{figure}
```

**Reference the figure:**
```latex
As shown in Figure \ref{fig:image_label}...
```

**Important notes:**
- Images should be in `Hinhve/` directory
- Supported formats: PNG, JPG, PDF
- Use `[H]` for "Here" placement (requires `float` package)
- All figures MUST be referenced and explained in the text

**Image without file extension:**
```latex
\includegraphics{Hinhve/Picture1}  % LaTeX finds .png/.jpg automatically
```

### Creating Tables

**Basic table:**
```latex
\begin{table}[H]
\centering
\begin{tabular}{|c|c|c|}
    \hline
    \textbf{Column 1} & \textbf{Column 2} & \textbf{Column 3} \\ \hline
    Data 1 & Data 2 & Data 3 \\ \hline
    Data 4 & Data 5 & Data 6 \\ \hline
\end{tabular}
\caption{Table caption}
\label{table:my_table}
\end{table}
```

**Reference the table:**
```latex
Table \ref{table:my_table} shows...
```

**Table tool recommendation:**
Use online generators like https://www.tablesgenerator.com/

**Landscape tables:**
For wide tables, use landscape mode (already included packages support this).

---

## Working with Acronyms and Glossary

### Defining Acronyms

Edit `Tu_viet_tat.tex`:

```latex
\newglossaryentry{acronym_key}{
    type=\acronymtype,
    name={SHORT},
    description={Full description in Vietnamese},
    first={Full form (Short - ABBR)}
}
```

**Examples from template:**

```latex
\newglossaryentry{API}{
    type=\acronymtype,
    name={API},
    description={Giao diện lập trình ứng dụng (Application Programming Interface)},
    first={API}
}

\newglossaryentry{HTML}{
    type=\acronymtype,
    name={HTML},
    description={Ngôn ngữ đánh dấu siêu văn bản (HyperText Markup Language)},
    first={Ngôn ngữ đánh dấu siêu văn bản (HyperText Markup Language - HTML)}
}
```

### Using Acronyms in Text

First, the template needs to generate the glossary. In `DoAn.tex` (line 243):
```latex
\glsaddall  % Include all defined acronyms
```

The glossary is printed automatically (line 248):
```latex
\printnoidxglossaries
```

**Note:** The template uses `\makenoidxglossaries` (line 2 of Tu_viet_tat.tex), which means you don't need to run external indexing tools.

---

## Formatting Conventions

### Page Layout

- **Paper size:** A4
- **Font:** Times New Roman, 13pt
- **Margins:**
  - Top: 2cm
  - Bottom: 2cm
  - Left: 3.5cm (for binding)
  - Right: 2.5cm

### Line Spacing and Paragraphs

Configured in `DoAn.tex` (lines 157-161):

```latex
\onehalfspacing          % 1.5 line spacing
\setlength{\parskip}{6pt}     % 6pt space between paragraphs
\setlength{\parindent}{15pt}  % 15pt first line indent
```

### Numbering Depth

```latex
\setcounter{secnumdepth}{3}  % Number down to subsubsection
```

### Figure and Table Numbering

Figures are numbered by chapter (line 146):
```latex
\counterwithin{figure}{chapter}  % e.g., Figure 1.1, 1.2, etc.
```

### Lists

**Unordered list (bullets):**
```latex
\begin{itemize}
\item First item
\item Second item
\end{itemize}
```

**Ordered list (numbers):**
```latex
\begin{enumerate}
\item First item
\item Second item
\end{enumerate}
```

**Custom list style:**
The template uses `enumitem` package for customization.

### Headers and Footers

Main content pages (lines 262-266):
```latex
\pagestyle{fancy}
\fancyhf{}                      % Clear all
\fancyhead[RE, LO]{\leftmark}  % Chapter name in header
\fancyfoot[RE, LO]{\thepage}   % Page number in footer
```

---

## Important Notes and Guidelines

### Writing Style Guidelines (from template comments)

1. **Academic Writing:**
   - NO bullet points or numbered lists in main text (write full paragraphs)
   - Avoid colloquial language
   - Avoid exaggerations like "tuyệt vời", "cực hay", "cực kỳ hữu ích"
   - Be concise and objective

2. **Paragraph Structure:**
   - One main idea per paragraph
   - Not too long
   - Sentences must be complete (subject + predicate)
   - Link sentences and paragraphs logically

3. **When bullets are necessary:**
   Use Roman numerals in scientific style:
   ```latex
   Many students feel regret because (i) they didn't try hard enough,
   (ii) they didn't manage time well, and (iii) they wrote carelessly.
   ```

### Citations and Plagiarism

**CRITICAL RULES:**
- Cite ALL sources that are not your original work
- This includes: quotes, figures, tables, ideas
- Plagiarism results in **disqualification from thesis defense**
- All figures, tables, equations MUST be referenced at least once in text

### Content Requirements

1. **All figures, tables, equations must be:**
   - Referenced in text
   - Explained/analyzed
   - Have proper captions

2. **Chapter descriptions in Section 1.4:**
   - Must be full paragraphs
   - NO bullet points
   - Don't describe Chapter 1

3. **Use this template as-is:**
   - Write directly in provided files
   - Don't create new files
   - When pasting content, use "Text Only" to preserve formatting
   - Maintain consistency throughout

### File Operations

**DO:**
- Edit existing `.tex` files in `Chuong/` directory
- Add images to `Hinhve/` directory
- Add references to `.bib` file

**DON'T:**
- Create new chapter files
- Modify overall structure
- Change font/margins without justification
- Write on a new file and copy over

### lstlisting Configuration

**Note:** The main file references `\include{lstlisting}` (line 62) but this file is not present. You may need to create it for code listings:

Create `lstlisting.tex`:
```latex
% Code listing configuration
\lstset{
    basicstyle=\ttfamily\small,
    keywordstyle=\color{blue}\bfseries,
    commentstyle=\color{gray}\itshape,
    stringstyle=\color{red},
    numbers=left,
    numberstyle=\tiny\color{gray},
    stepnumber=1,
    numbersep=10pt,
    backgroundcolor=\color{white},
    showspaces=false,
    showstringspaces=false,
    showtabs=false,
    frame=single,
    tabsize=2,
    captionpos=b,
    breaklines=true,
    breakatwhitespace=false,
    escapeinside={\%*}{*)},
}
```

### Compiling Issues

**Common issues:**

1. **Missing lstlisting file:**
   - Create the file or comment out line 62 in DoAn.tex

2. **Bibliography not showing:**
   - Ensure you run bibtex
   - Check that references are cited with `\cite{}`

3. **Images not found:**
   - Verify image path in `\graphicspath`
   - Check image file names (case-sensitive on Linux/Mac)

4. **Vietnamese characters not displaying:**
   - Ensure UTF-8 encoding
   - Check `\usepackage[utf8]{vietnam}` is loaded

### Program Names (from Phu_luc_A.tex)

Use correct program names on cover:

**For Regular Engineering (Kỹ sư chính quy):**
- K61 and before: Ngành Kỹ thuật phần mềm
- K62 and after: Ngành Khoa học máy tính

**For Bachelor (Cử nhân):**
- Ngành Công nghệ thông tin

**For EliteTech programs:**
- Việt Nhật/KSTN: Ngành Công nghệ thông tin
- ICT Global: Ngành Information Technology
- DS&AI: Ngành Khoa học dữ liệu

---

## Additional Resources

**LaTeX Learning:**
- Overleaf Documentation: https://www.overleaf.com/learn
- Table Generator: https://www.tablesgenerator.com/
- LaTeX Lists: https://www.overleaf.com/learn/latex/Lists
- LaTeX Tables: https://www.overleaf.com/learn/latex/Tables
- LaTeX Images: https://www.overleaf.com/learn/latex/Inserting_Images

**Template-Specific:**
- Read **Appendix A** (Phu_luc_A.tex) carefully for detailed writing guidelines
- All instructions apply to ALL thesis types, not just application projects

---

## Quick Start Checklist

- [ ] Update `\TITLE` and `\AUTHOR` in DoAn.tex
- [ ] Edit Bia.tex and Bia_lot.tex with personal information
- [ ] Write acknowledgments in `0_2_Loi_cam_on.tex`
- [ ] Write Vietnamese abstract in `0_3_Tom_tat_noi_dung.tex`
- [ ] Write English abstract in `0_4_Tom_tat_noi_dung_English.tex`
- [ ] Add acronyms to `Tu_viet_tat.tex`
- [ ] Write chapter content in files in `Chuong/` directory
- [ ] Add references to `Danh_sach_tai_lieu_tham_khao.bib`
- [ ] Place images in `Hinhve/` directory
- [ ] Create `lstlisting.tex` if you need code listings
- [ ] Compile: pdflatex → bibtex → pdflatex → pdflatex
- [ ] Review Appendix A for detailed writing guidelines

---

## Support

For issues related to:
- **Template formatting:** Check Appendix A in the compiled PDF
- **LaTeX syntax:** Consult Overleaf documentation
- **Thesis requirements:** Contact your advisor or check university guidelines

**Good luck with your graduation thesis!**

---

## Detailed Chapter Instructions

This section provides detailed guidelines for each chapter file based on the template structure and embedded comments. These instructions help you understand what content belongs in each file and what specific requirements must be met.

### 0_2_Loi_cam_on.tex - Acknowledgments

**Purpose:** Acknowledgments section

**Content Guidelines:**
- Write brief acknowledgments to loved ones, family, friends, teachers, and yourself
- Express gratitude for hard work and determination in completing the thesis
- Keep it concise and avoid empty, cliché phrases
- Target length: 100-150 words

### 0_3_Tom_tat_noi_dung.tex - Vietnamese Abstract

**Purpose:** Vietnamese abstract of the thesis

**Content Guidelines:**
- Target length: 200-350 words
- Must include the following in order:
  - (i) Problem introduction: Why the problem exists, current solutions, different approaches, how they solve it, and their limitations
  - (ii) Your chosen approach and why you selected it
  - (iii) Overview of your solution following the chosen approach
  - (iv) Main contributions and final results achieved
- **IMPORTANT:** Must write in full paragraphs, absolutely NO bullet points or numbered lists
- Must sign at the end with your name

### 0_4_Tom_tat_noi_dung_English.tex - English Abstract

**Purpose:** English translation of the Vietnamese abstract

**Content Guidelines:**
- This section is **optional but encouraged**
- Must include all the same content as the Vietnamese abstract
- **CRITICAL:** If you choose to include this section, ensure proper grammar and sentence structure
- Poor English will have a negative effect on evaluation
- If unsure about English quality, it's better to skip this section

### 0_5_Danh_muc_viet_tat.tex - List of Abbreviations

**Purpose:** Table of abbreviations used in the thesis

**Structure:**
- Three-column table: Abbreviation | English Name | Vietnamese Name
- Uses `longtable` environment for tables that may span multiple pages
- Add all acronyms and abbreviations used throughout your thesis

### 0_6_Thuat_ngu.tex - Terminology/Glossary

**Purpose:** Technical terms and their translations

**Structure:**
- Two-column table: English Term | Vietnamese Translation
- Includes technical terms that need explanation or translation
- Helps readers understand domain-specific vocabulary

### 1_Gioi_thieu.tex - Chapter 1: Introduction

**Purpose:** Introduce the problem, objectives, and solution approach

**Length:** 3-6 pages

**Important Instructions:**
- **MUST READ:** Before writing the thesis, carefully read all detailed guidelines in Appendix A (Phu_luc_A.tex)
- Use this template directly - write into this file, only modify content, do not create new files
- When pasting content from other documents, use "Text Only" to preserve template formatting
- Pay special attention to writing style: paragraphs should not be too long, contain one main idea, have complete sentences with subject and predicate

**Chapter Structure:**

**Section 1.1 - Đặt vấn đề (Problem Statement):**
- Highlight the urgency, importance, and/or scale of your problem
- Start from real-world situation leading to the problem
- Explain benefits if the problem is solved, who benefits, and potential applications
- **Note:** Only present the problem, absolutely DO NOT present the solution here

**Section 1.2 - Mục tiêu và phạm vi đề tài (Objectives and Scope):**
- First, present overview of current research results (for research projects) or current products/user needs (for application projects)
- Compare and evaluate these products/research
- Summarize current limitations
- State specific problems you will solve, limitations you will address, **main functions** of your software
- **Note:** Only present overview, do not go into details (details in later chapters, especially Chapter 5)

**Section 1.3 - Định hướng giải pháp (Solution Approach):**
- Follow this sequence:
  - (i) State which direction, method, algorithm, technique, or technology you will use
  - (ii) Briefly describe your solution (following the stated approach)
  - (iii) State main contributions and results achieved
- **Note:** Do not explain or analyze technology/algorithm in detail here - just name it, describe briefly in 1-2 sentences, and quickly explain why you chose it

**Section 1.4 - Bố cục đồ án (Thesis Structure):**
- Describe the structure of remaining chapters
- **CRITICAL:** Must write in full paragraphs - absolutely NO bullet points or numbered lists
- Do NOT describe Chapter 1 in this section
- Example format: "Chapter 2 presents about...", "In Chapter 3, I introduce..."

**General Chapter Guidelines (applies to all chapters):**
- Each chapter should have **Tổng quan (Overview)** at the beginning:
  - Link to previous chapter
  - Explain why this chapter is necessary
  - Preview what will be covered
- Each chapter should have **Kết chương (Chapter Conclusion)** at the end:
  - Summarize key points
  - Don't repeat the overview verbatim
  - Provide transition to next chapter

### 2_Khao_sat.tex - Chapter 2: Survey and Analysis

**Purpose:** Survey current state, analyze requirements, and specify system functions

**Length:** 9-11 pages

**Important Instructions:**
- For object-oriented analysis and design, use use case diagrams as guided
- For other methodologies (e.g., Agile), consult your advisor to modify sections (e.g., use User Stories instead of use cases)
- Each chapter should have opening paragraph introducing content and closing paragraph summarizing content

**Section 2.1 - Khảo sát hiện trạng (Current State Survey):**
- Survey from three main sources:
  - (i) Users/customers
  - (ii) Existing systems
  - (iii) Similar applications
- Analyze, compare, and evaluate advantages and disadvantages of existing products/research in detail
- Can create comparison tables if necessary
- Combined with user/customer surveys (if any), list and briefly describe important software features to be developed

**Section 2.2 - Tổng quan chức năng (Function Overview):**
- Summarize software functions at high level
- **Note:** Only describe high-level functions, NOT detailed specifications (details in Section 2.3)

**Subsection 2.2.1 - Biểu đồ use case tổng quát (General Use Case Diagram):**
- Draw overview use case diagram
- Explain what actors are involved and their roles
- Briefly describe main use cases

**Subsection 2.2.2 - Biểu đồ use case phân rã (Decomposed Use Cases):**
- For each high-level use case in the general diagram, create a separate subsection
- **IMPORTANT:** Use case name in general diagram must match the subsection title
- Draw and briefly explain decomposed use cases

**Subsection 2.2.3 - Quy trình nghiệp vụ (Business Process):**
- If the system has important/notable business processes, describe and draw activity diagrams
- **Note:** This is NOT event flow of individual use cases, but workflow combining multiple use cases for a business process
- Example: Library system borrowing process involving multiple use cases (register card → request book → librarian approves → return book)

**Section 2.3 - Đặc tả chức năng (Function Specification):**
- Select 4-7 most important use cases for detailed specification
- Each specification must include at least:
  - (i) Use case name
  - (ii) Event flow (main and alternative)
  - (iii) Preconditions
  - (iv) Postconditions
- Only draw activity diagrams for complex use cases

**Section 2.4 - Yêu cầu phi chức năng (Non-functional Requirements):**
- List other requirements including non-functional requirements (performance, reliability, usability, maintainability)
- Include technical requirements (database, technologies to be used, etc.)

### 3_Cong_nghe.tex - Chapter 3: Technologies Used / Theoretical Foundations

**Purpose:** Introduce technologies, platforms, and theoretical foundations

**Length:** Maximum 10 pages (if longer, move additional content to appendix)

**Important Instructions:**
- **For application projects:** Keep chapter title as "Công nghệ sử dụng" (Technologies Used)
- **For research projects:** Change chapter title to "Cơ sở lý thuyết" (Theoretical Foundations)
- This chapter contains existing knowledge - after researching, analyze and summarize
- Do NOT write in long-winded detail

**Content Requirements:**
- For application projects: Introduce technologies and platforms used in the project; can also present theoretical foundations if needed
- For research projects: Present foundational knowledge, theoretical basis, algorithms, research methods, etc.
- **For each technology/platform/theory presented:**
  - Must clearly analyze which specific problem/requirement from Chapter 2 it solves
  - Must list alternative technologies/approaches that could be used instead
  - Must explain your choice clearly
- **IMPORTANT:** Content must be connected, seamless, and consistent - technologies/algorithms presented here must match what you introduced in earlier sections
- To increase scientific value and credibility, cite sources for information and add them to references

### 4_Ket_qua_thuc_nghiem.tex - Chapter 4: Design and Implementation

**Purpose:** Present architecture design, detailed design, implementation, testing, and deployment

**Section 4.1 - Thiết kế kiến trúc (Architecture Design)**

**Subsection 4.1.1 - Lựa chọn kiến trúc phần mềm (Software Architecture Selection):**
- Length: 1-3 pages
- Choose software architecture for your application (three-tier, MVC, MVP, SOA, Microservice, etc.)
- Explain briefly about the architecture (not in detail/lengthy)
- Describe specific architecture for your application
- Suggestion: Explain how you apply general theory to your system/product, any modifications, additions, or improvements
- Example: What specific components will the M in theoretical MVC architecture be (e.g., interface I + class C1 + class C2, etc.) in your software architecture

**Subsection 4.1.2 - Thiết kế tổng quan (Overview Design):**
- Draw UML package diagram showing dependencies between packages
- Must arrange packages clearly by layers, do NOT arrange packages haphazardly in the diagram
- Follow design rules:
  - Packages should not depend on each other cyclically
  - Lower layer packages should not depend on upper layer packages
  - No skipping layers in dependencies
- Briefly explain purpose/responsibilities of each package
- See example in Figure 1 of the template

**Subsection 4.1.3 - Thiết kế chi tiết gói (Detailed Package Design):**
- Design and draw design diagrams for each package, or a group of related packages to solve a specific problem
- When drawing package design, only include class names, no need to show member methods and attributes
- Clearly draw relationships between classes: dependency, association, aggregation, composition, inheritance, implementation
- After drawing, briefly explain your design
- See example in Figure 2 of the template

**Section 4.2 - Thiết kế chi tiết (Detailed Design)**

**Subsection 4.2.1 - Thiết kế giao diện (Interface Design):**
- Length: 2-3 pages
- Specify screen information: screen resolution, screen size, number of colors supported, etc.
- State your standards/conventions for interface design: button design, controls, message display location, color scheme, etc.
- Provide mockup images for most important functions
- **Note:** Do not confuse design mockups with final product interface

**Subsection 4.2.2 - Thiết kế lớp (Class Design):**
- Length: 3-4 pages
- Present detailed design of attributes and methods for 2-4 most important/key classes
- Detailed design for other classes can be put in appendix if desired
- To illustrate class design, design message flow between participating objects for 2-3 important use cases using sequence diagrams (or communication diagrams)

**Subsection 4.2.3 - Thiết kế cơ sở dữ liệu (Database Design):**
- Length: 2-4 pages
- Design, draw, and explain Entity-Relationship (E-R) diagram
- From that, design database according to your chosen DBMS (SQL, NoSQL, Firebase, etc.)

**Section 4.3 - Xây dựng ứng dụng (Application Development)**

**Subsection 4.3.1 - Thư viện và công cụ sử dụng (Libraries and Tools Used):**
- List tools, programming languages, APIs, libraries, IDE, testing tools, etc.
- Each tool must specify version used
- Should create a table with columns: Purpose | Tool | URL

**Subsection 4.3.2 - Kết quả đạt được (Results Achieved):**
- Describe what results you achieved: packaged products, components, their meaning and role
- Provide statistics: lines of code, number of classes, number of packages, total source code size, package sizes, etc.
- Use tables to present statistical information

**Subsection 4.3.3 - Minh họa các chức năng chính (Main Function Illustrations):**
- Select and show screens for main, important, and most interesting functions
- Each interface needs brief explanation
- Can combine with annotations in the interface images

**Section 4.4 - Kiểm thử (Testing):**
- Length: 2-3 pages
- Design test cases for 2-3 most important functions
- Must clearly specify testing techniques used
- Detail other test cases in appendix if desired
- Summarize number of test cases and test results
- Must analyze reasons if test results fail

**Section 4.5 - Triển khai (Deployment):**
- Present deployment model and/or experimental/actual deployment methods
- Specify server/device for deployment and configuration
- Include experimental deployment results if available: number of users, number of accesses, response time, user feedback, load capacity, statistics, etc.

### 5_Giai_phap_dong_gop.tex - Chapter 5: Solutions and Contributions

**Purpose:** Present all your main contributions and innovative solutions

**Length:** Minimum 5 pages, no maximum limit

**Critical Instructions:**
- **If this section is less than 5 pages, merge it with conclusion instead of making it a separate chapter**
- **This is THE MOST IMPORTANT chapter for evaluation** - professors will base their assessment primarily on this chapter
- Demonstrate creativity, analytical ability, critical thinking, reasoning, and problem generalization
- Focus and write this chapter very well

**Content Requirements:**
- Present ALL contributions you are most proud of during the thesis work
- This could be:
  - A series of difficult problems you solved step by step
  - Algorithms for specific problems
  - General solutions for a class of problems
  - Effective models/architectures you designed

**Structure for Each Solution/Contribution:**
Each solution or contribution must be presented in an independent section with three subsections:
- (i) Introduction to the problem
- (ii) Your solution
- (iii) Results achieved (if applicable)

**CRITICAL - Avoid Repetition:**
- **DO NOT repeat content** already presented in detail in previous chapters
- Content already detailed in earlier chapters should NOT be repeated here
- For good content with contributions/solutions:
  - Only briefly summarize/describe in earlier chapters
  - Create cross-reference to the corresponding section in Chapter 5
  - Present full details in Chapter 5
- Example: If you designed a notable architecture in Chapter 4 (combination of MVC, MVP, SOA, etc.), briefly describe it in Chapter 4, then add: "Details about this architecture will be presented in Section 5.1"

### 6_Ket_luan.tex - Chapter 6: Conclusion

**Purpose:** Conclude the thesis and present future work

**Section 6.1 - Kết luận (Conclusion):**
- Compare your research results or product with similar research/products
- Analyze throughout the thesis process:
  - What you accomplished
  - What you have not yet accomplished
  - Notable contributions
  - Lessons learned and experiences gained

**Section 6.2 - Hướng phát triển (Future Work):**
- Present future work directions to complete your product or research
- First, present necessary work to complete existing functions/tasks
- Then analyze new directions that allow improvement and upgrade of completed functions/tasks

### 7_Luu_y_tai_lieu_tham_khao.tex - Notes on References

**Purpose:** Guidelines for properly formatting references

**Critical Rules:**
- **CANNOT** use lecture slides, Wikipedia, or regular websites as references
- A website is allowed as a reference **ONLY IF** it is an official publication from an individual or organization
- Example of valid web reference: W3C's XML specification at https://www.w3.org/TR/2008/REC-xml-20081126/

**Five Types of References with Required Formats:**

1. **Journal Articles:**
   - Format: Author name, article title, journal name, volume, page numbers (if available), publisher, publication year
   - Example provided in the file

2. **Books:**
   - Format: Author name, book title, volume (if any), edition (if any), publisher, publication year
   - Example provided in the file

3. **Conference Papers:**
   - Format: Author name, paper title, conference name, date (if any), conference location, publication year
   - Example provided in the file

4. **Theses/Dissertations:**
   - Format: Author name, thesis/dissertation title, type of thesis/dissertation, university name, location, publication year
   - Example provided in the file

5. **Internet Resources:**
   - Format: Author name (if available), title, organization (if available), website URL, last access date
   - Example provided in the file

### Phu_luc_A.tex - Appendix A: Thesis Writing Guidelines

**Purpose:** Comprehensive guidelines and regulations for writing the thesis

**Important Sections:**

**Section A.1 - General Regulations:**
- Ensure consistency throughout the report (font, alignment, images, tables, margins, page numbering, etc.)
- Use template formatting - when pasting content, use "Text Only" to preserve formatting
- **ABSOLUTELY FORBIDDEN:** Plagiarism - must cite sources for everything not your own (quotes, images, tables, etc.)
- Plagiarism discovery results in **disqualification from thesis defense**
- All figures, tables, formulas, and references MUST be explained and cross-referenced at least once
- **DO NOT use bullet points or numbered lists in main text** - write full paragraphs
- Avoid colloquial language and exaggerations ("tuyệt vời", "cực hay", "cực kỳ hữu ích")
- When listing is absolutely necessary, use Roman numerals in scientific style: "(i) item one, (ii) item two, (iii) item three"
- Sentences must be complete with subject and predicate
- Each paragraph should contain one main idea, not too long
- Link sentences and paragraphs logically

**Section A.2 - Program Names:**
- For Regular Engineering (Kỹ sư chính quy):
  - K61 and before: "Ngành Kỹ thuật phần mềm"
  - K62 and after: "Ngành Khoa học máy tính"
- For Bachelor (Cử nhân): "Ngành Công nghệ thông tin"
- For EliteTech programs:
  - Việt Nhật/KSTN: "Ngành Công nghệ thông tin"
  - ICT Global: "Ngành Information Technology"
  - DS&AI: "Ngành Khoa học dữ liệu"

**Section A.3 - Lists (Bullet and Numbering):**
- LaTeX provides two list environments:
  - `itemize` for unordered lists (bullets)
  - `enumerate` for ordered lists (numbering)
- Reference: https://www.overleaf.com/learn/latex/Lists

**Section A.4 - Tables:**
- All tables must be referenced in content and analyzed/commented on
- Creating tables in LaTeX is complex - use online tools like https://www.tablesgenerator.com/
- Reference: https://www.overleaf.com/learn/latex/Tables

**Section A.5 - Images:**
- All figures must be referenced in content and analyzed/commented on
- Caption is placed directly under the figure
- Reference: https://www.overleaf.com/learn/latex/Inserting_Images

**Section A.6 - References:**
- Apply IEEE citation format
- Use `\cite{}` command to cite
- Only cited documents appear in references section
- References must have clear origin and be from reliable sources
- Limit citations from websites and Wikipedia

**Section A.7 - Mathematical Equations and Formulas:**
- Packages amsmath, amssymb, amsfonts support mathematical equations
- Can create numbered or unnumbered equations

**Section A.9 - Binding Specifications:**
- Front and back covers: full-sheet paper
- Use thermal glue for spine binding (not tape and staples)
- Spine must include: Semester - Program - Student Name - Student ID
- Example: "2022.1 - KỸ THUẬT MÁY TÍNH - NGUYỄN VĂN A - 20221234"

### Phu_luc_B.tex - Appendix B: Use Case Specifications

**Purpose:** Additional use case specifications

**Content Guidelines:**
- If there is not enough space in the main content (Chapter 2, Section 2.3) for all use case specifications, add them here
- Each use case should have its own section
- Format same as in Chapter 2: include use case name, event flow, preconditions, postconditions
