package 
{
	[struct]
	public final class FeaturePair 
	{
		public var e:Edges = new Edges();
		
		public function get value():int
		{
			return e.inEdge1 | (e.outEdge1 << 8) | (e.inEdge2 << 16) | (e.outEdge2 << 24);
		}		
	}

}