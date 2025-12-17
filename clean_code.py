import ast
import tokenize
import io
import os
from pathlib import Path
from typing import List, Tuple

class DocstringRemover(ast.NodeTransformer):
    """Remove docstrings from AST nodes."""

    def visit_Module(self, node):
        self.generic_visit(node)
        if node.body and isinstance(node.body[0], ast.Expr) and isinstance(node.body[0].value, (ast.Str, ast.Constant)):
            if isinstance(node.body[0].value, ast.Constant) and isinstance(node.body[0].value.value, str):
                node.body.pop(0)
            elif isinstance(node.body[0].value, ast.Str):
                node.body.pop(0)
        return node

    def visit_FunctionDef(self, node):
        self.generic_visit(node)
        if node.body and isinstance(node.body[0], ast.Expr) and isinstance(node.body[0].value, (ast.Str, ast.Constant)):
            if isinstance(node.body[0].value, ast.Constant) and isinstance(node.body[0].value.value, str):
                node.body.pop(0)
            elif isinstance(node.body[0].value, ast.Str):
                node.body.pop(0)
        if not node.body:
            node.body = [ast.Pass()]
        return node

    def visit_AsyncFunctionDef(self, node):
        return self.visit_FunctionDef(node)

    def visit_ClassDef(self, node):
        self.generic_visit(node)
        if node.body and isinstance(node.body[0], ast.Expr) and isinstance(node.body[0].value, (ast.Str, ast.Constant)):
            if isinstance(node.body[0].value, ast.Constant) and isinstance(node.body[0].value.value, str):
                node.body.pop(0)
            elif isinstance(node.body[0].value, ast.Str):
                node.body.pop(0)
        if not node.body:
            node.body = [ast.Pass()]
        return node

def remove_comments_from_source(source_code: str) -> str:
    """Remove comments from source code while preserving strings."""
    result = []
    try:
        tokens = tokenize.generate_tokens(io.StringIO(source_code).readline)
        for token_type, token_string, start, end, line in tokens:
            if token_type != tokenize.COMMENT:
                result.append((token_type, token_string, start, end, line))
        return tokenize.untokenize(result)
    except Exception as e:
        print(f"Error removing comments: {e}")
        return source_code

def clean_empty_lines(source_code: str) -> str:
    """Clean up excessive empty lines, allowing max 1 blank line between code blocks."""
    lines = source_code.split('\n')
    result = []
    empty_count = 0

    for line in lines:
        if line.strip() == '':
            empty_count += 1
            if empty_count <= 1:
                result.append(line)
        else:
            empty_count = 0
            result.append(line)

    while result and result[-1].strip() == '':
        result.pop()

    return '\n'.join(result)

def process_python_file(file_path: Path) -> Tuple[bool, str]:
    """Process a single Python file to remove docstrings and comments."""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            original_source = f.read()

        if not original_source.strip():
            return False, "Empty file"

        try:
            tree = ast.parse(original_source)
        except SyntaxError as e:
            return False, f"Syntax error: {e}"

        remover = DocstringRemover()
        new_tree = remover.visit(tree)
        ast.fix_missing_locations(new_tree)

        code_without_docstrings = ast.unparse(new_tree)

        code_without_comments = remove_comments_from_source(code_without_docstrings)

        final_code = clean_empty_lines(code_without_comments)

        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(final_code)

        return True, "Success"

    except Exception as e:
        return False, f"Error: {str(e)}"

def find_python_files(root_dir: str) -> List[Path]:
    """Recursively find all Python files in the directory."""
    python_files = []
    for root, dirs, files in os.walk(root_dir):
        dirs[:] = [d for d in dirs if not d.startswith('.') and d not in ['__pycache__', 'venv', 'env', 'node_modules']]

        for file in files:
            if file.endswith('.py') and not file.startswith('.'):
                python_files.append(Path(root) / file)

    return python_files

def main():
    """Main function to clean all Python files."""
    current_dir = os.getcwd()
    print(f"Scanning directory: {current_dir}")
    print("=" * 80)

    python_files = find_python_files(current_dir)

    if not python_files:
        print("No Python files found.")
        return

    print(f"Found {len(python_files)} Python file(s)")
    print("=" * 80)

    success_count = 0
    failed_count = 0
    skipped_files = ['clean_code.py']

    for file_path in python_files:
        if file_path.name in skipped_files:
            print(f"SKIPPED: {file_path} (cleanup script itself)")
            continue

        success, message = process_python_file(file_path)

        if success:
            success_count += 1
            print(f"CLEANED: {file_path}")
        else:
            failed_count += 1
            print(f"FAILED: {file_path} - {message}")

    print("=" * 80)
    print(f"Summary:")
    print(f"  Successfully cleaned: {success_count}")
    print(f"  Failed: {failed_count}")
    print(f"  Total processed: {success_count + failed_count}")

if __name__ == "__main__":
    main()
