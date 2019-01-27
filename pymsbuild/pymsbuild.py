import ast

indent = 0
indentval = "  "

def err(why):
    print(why, file=sys.stderr)

class Xml:
    def __init__(self, name, **kwargs):
        self.name = name
        self.args = kwargs

    def __enter__(self):
        global indent
        print((indentval * indent) + '<{}'.format(self.name))
        for key, value in self.args.items():
            print((indentval * indent) + '  {}="{}"'.format(key, value))
        print((indentval * indent) + "  >")
        indent = indent + 1

    def __exit__(self, ty, val, tr):
        global indent
        indent = indent - 1
        print((indentval * indent) + '</{}>'.format(self.name))

class Compiler(ast.NodeVisitor):
    def __init__(self):
        pass

    def visit_FunctionDef(self, node):
        with Xml("Target",
                Name=node.name,
                Returns=""):
            self.generic_visit(node)
        return None

    def visit_Call(self, node):
        ident = self.visit(node.func)
        if ident is None:
            err("Call didn't have ident as function name")
            return None
        if ident is "print":
            with Xml("Message",
                    Text="BONK from {}".format(str(node.args))):
                pass
            return None
        with Xml("CallTarget",
                Targets=ident):
            pass
        return self.generic_visit(node)

    def visit_Name(self, node):
        return str(node.id)

def compile_file(filename):
    with open(filename, 'r') as f:
        compile_str(f.read())

def compile_str(string):
    return compile(ast.parse(string))

def compile(tree):
    with Xml("Project",
            DefaultTargets="Main"):
        Compiler().visit(tree)

if __name__ == "__main__":
    import sys
    compile_file(sys.argv[1])
