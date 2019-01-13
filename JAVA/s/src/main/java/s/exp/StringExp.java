package s.exp;

import s.Token;

public class StringExp extends AtomExp{
    private String value;

    public StringExp(Token block, String value) {
        super(block);
        this.value=value;
    }

    public String getValue(){
        return value;
    }
}
