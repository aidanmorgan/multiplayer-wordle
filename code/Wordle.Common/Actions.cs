namespace Wordle.Common;

public class Actions
{
    public static Action<T> callOnlyOnce<T>(Action<T> action){
        var context = new ContextCallOnlyOnce();
        
        Action<T> ret = (builder)=>{
            if(false == context.AlreadyCalled){
                action(builder);
                context.AlreadyCalled = true;
            }
        };

        return ret;
    }

    class ContextCallOnlyOnce{
        public bool AlreadyCalled;
    }

}