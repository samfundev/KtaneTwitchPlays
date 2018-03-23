public class Tuple<T1, T2>
{
	public T1 First { get; private set; }
	public T2 Second { get; private set; }
	internal Tuple(T1 first, T2 second)
	{
		First = first;
		Second = second;
	}
}

public class Tuple<T1, T2, T3>
{
	public T1 First { get; private set; }
	public T2 Second { get; private set; }
	public T3 Third { get; private set; }
	internal Tuple(T1 first, T2 second, T3 third)
	{
		First = first;
		Second = second;
		Third = third;
	}
}

public class Tuple<T1, T2, T3, T4>
{
	public T1 First { get; private set; }
	public T2 Second { get; private set; }
	public T3 Third { get; private set; }
	public T4 Fourth { get; private set; }
	internal Tuple(T1 first, T2 second, T3 third, T4 fourth)
	{
		First = first;
		Second = second;
		Third = third;
		Fourth = fourth;
	}
}