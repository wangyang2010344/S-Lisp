package s.exp;
import s.Token;

public abstract class AtomExp extends Exp{
    protected Token token;
    @Override
    public s.Location Loc() {
    	return token.Loc();
    }
	@Override
	public String toString() {
		StringBuilder sb=new StringBuilder();
		toString(sb);
		return sb.toString();
	}
	@Override
	public String toString(int indent) {
		StringBuilder sb=new StringBuilder();
        repeat(sb,indent);
		toString(sb);
		return sb.toString();
	}
	@Override
	protected void toString(StringBuilder sb, int indent) {
		// TODO Auto-generated method stub
        repeat(sb,indent);
		toString(sb);
	}
    protected void toString(StringBuilder sb,Object value,String before,String after){
        sb.append(before).append(value).append(after);
    }
	@Override
	public boolean isBracket() {
		// TODO Auto-generated method stub
		return false;
	}
}
