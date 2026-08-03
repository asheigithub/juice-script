using juicescript;
using juicescript.ABC;
using juicescript.runtime;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Drawing;
using System.Reflection;

namespace box2dlite
{
	internal class Program
	{
		private static IWindow window;
		private static GL Gl;


		static float zoom = 10.0f;
		static float pan_y = 8.0f;

		static void Main(string[] args)
		{
			//Create a window.
			var options = WindowOptions.Default;
			options.Size = new Vector2D<int>(1280, 720);
			options.Title = "box2dlite";

			window = Window.Create(options);

			//Assign events.
			window.Load += OnLoad;
			window.Update += OnUpdate;
			window.Render += OnRender;
			window.FramebufferResize += OnFramebufferResize;
			window.Closing += OnClosing
				;
			
			//Run the window.
			window.Run();

			// window.Run() is a BLOCKING method - this means that it will halt execution of any code in the current
			// method until the window has finished running. Therefore, this dispose method will not be called until you
			// close the window.
			window.Dispose();
		}

		private static void OnClosing()
		{
			Gl.DeleteBuffer(Vbo);
			Gl.DeleteBuffer(Ebo);
			Gl.DeleteVertexArray(Vao);
			Gl.DeleteProgram(Shader);
		}

		private static uint Vbo;
		private static uint Ebo;
		private static uint Vao;
		private static uint Shader;


		private static uint VboLine;	
		private static uint EboLine;
		private static uint VaoLine;


		//Vertex shaders are run on each vertex.
		private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec4 vPos;
        
		uniform mat4 _MVP;
        void main()
        {
            gl_Position = _MVP * vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";

		//Fragment shaders are run on each fragment/pixel of the geometry.
		private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
		
		uniform vec4 _Color;
        void main()
        {
            FragColor = _Color;
        }
        ";

		//Vertex data, uploaded to the VBO.
		private static readonly float[] Vertices =
		{
            //X    Y      Z
             0.5f,  0.5f, 0.0f,
			 0.5f, -0.5f, 0.0f,
			-0.5f, -0.5f, 0.0f,
			-0.5f,  0.5f, 0.0f
		};

		//Index data, uploaded to the EBO.
		private static readonly uint[] Indices =
		{
			0,1,2,3
		};

		private static Player Player;

		private static ASMethod Step;

		private static RtInstance world;

		private static int body_rotation;
		private static int body_positoin;
		private static int body_width;


		private static int joint_body1;
		private static int joint_body2;
		private static int joint_localAnchor1;
		private static int joint_localAnchor2;


		private static int arbiter_numContacts;
		private static int arbiter_contacts;


		private unsafe static void OnLoad()
		{
			//加载脚本播放器

			Player = new Player();
			//加载全局swc
			{
				var path = Assembly.GetExecutingAssembly().Location;
				var i = path.IndexOf("samples");
				path = path.Substring(0, i);

				string global_swc_path = path + "player\\bin\\Debug\\net6.0\\juice_global.swc";

				Player.LoadLib( System.IO.File.ReadAllBytes(global_swc_path) );
			}
			//加载box2d-lite脚本
			{
				var path = Assembly.GetExecutingAssembly().Location;
				var i = path.IndexOf("samples");
				path = path.Substring(0, i);

				string box2dlite_swc_path = path + "fd_projs\\dev_scripts\\box2d-lite\\obj\\o.swc";

				Player.LoadLib(File.ReadAllBytes(box2dlite_swc_path));

				bool error = false;

				Player.Run((ex) => {
					Console.Error.WriteLine(ex.Message);
					window.Close();
					error = true;
				});

				if (error)
					return;

				var body = Player.Context.libs.SelectMany(l => l.Classes).First(c => c != null && c.QName.Name == "Body").Instance;


				body_rotation = body._link_codescope.Members.FindIndex(m => m.QName.Name == "rotation");
				body_positoin = body._link_codescope.Members.FindIndex(m => m.QName.Name == "position");
				body_width = body._link_codescope.Members.FindIndex(m => m.QName.Name == "width");



				var joint = Player.Context.libs.SelectMany(l => l.Classes).First(c => c != null && c.QName.Name == "Joint").Instance;
				joint_body1 = joint._link_codescope.Members.FindIndex(m => m.QName.Name == "body1");
				joint_body2 = joint._link_codescope.Members.FindIndex(m => m.QName.Name == "body2");
				joint_localAnchor1 = joint._link_codescope.Members.FindIndex(m => m.QName.Name == "localAnchor1");
				joint_localAnchor2 = joint._link_codescope.Members.FindIndex(m => m.QName.Name == "localAnchor2");


				var arbiter = Player.Context.libs.SelectMany(l => l.Classes).First(c => c != null && c.QName.Name == "Arbiter").Instance;

				arbiter_numContacts = arbiter._link_codescope.Members.FindIndex(m => m.QName.Name == "numContacts");
				arbiter_contacts = arbiter._link_codescope.Members.FindIndex(m => m.QName.Name == "contacts");


				var main = Player.Context.libs.SelectMany(l => l.Classes).First(c=> c != null && c.QName.Name =="Main");

				ASScript doc = (ASScript)main._link_codescope.Parent.Container;

				int index = doc._link_codescope.Members.FindIndex( m=>m.QName.Name =="world" ) ;

				NaNBoxing world_value = ((RtScriptClass)Player.Context.GC.Heap[doc.__global_index__]).ReadSlot(index);
				world = (RtInstance)Player.Context.GC.Heap[world_value.HeapPtr];


				var demo = main.Traits.First(t=>t.QName.Name == "Demo1").Method;
				
				Player.InvokeStaticMethod(demo);

				Step = main.Traits.First(t => t.QName.Name == "Step").Method;

			}





			//Set-up input context.
			IInputContext input = window.CreateInput();
			for (int i = 0; i < input.Keyboards.Count; i++)
			{
				input.Keyboards[i].KeyDown += KeyDown;
			}

			Gl = GL.GetApi(window);

			Gl.Viewport(window.Size);


			//Creating a vertex array.
			Vao = Gl.GenVertexArray();
			Gl.BindVertexArray(Vao);

			//Initializing a vertex buffer that holds the vertex data.
			Vbo = Gl.GenBuffer(); //Creating the buffer.
			Gl.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo); //Binding the buffer.
			
			fixed (void* v = &Vertices[0])
			{
				Gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(Vertices.Length * sizeof(uint)), v, BufferUsageARB.StaticDraw); //Setting buffer data.
			}
			
			//Initializing a element buffer that holds the index data.
			Ebo = Gl.GenBuffer(); //Creating the buffer.
			Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, Ebo); //Binding the buffer.
			fixed (void* i = &Indices[0])
			{
				Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(Indices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw); //Setting buffer data.
			}


			//Creating a vertex shader.
			uint vertexShader = Gl.CreateShader(ShaderType.VertexShader);
			Gl.ShaderSource(vertexShader, VertexShaderSource);
			Gl.CompileShader(vertexShader);

			//Checking the shader for compilation errors.
			string infoLog = Gl.GetShaderInfoLog(vertexShader);
			if (!string.IsNullOrWhiteSpace(infoLog))
			{
				Console.WriteLine($"Error compiling vertex shader {infoLog}");
			}

			//Creating a fragment shader.
			uint fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
			Gl.ShaderSource(fragmentShader, FragmentShaderSource);
			Gl.CompileShader(fragmentShader);

			//Checking the shader for compilation errors.
			infoLog = Gl.GetShaderInfoLog(fragmentShader);
			if (!string.IsNullOrWhiteSpace(infoLog))
			{
				Console.WriteLine($"Error compiling fragment shader {infoLog}");
			}

			//Combining the shaders under one shader program.
			Shader = Gl.CreateProgram();
			Gl.AttachShader(Shader, vertexShader);
			Gl.AttachShader(Shader, fragmentShader);
			Gl.LinkProgram(Shader);

			//Checking the linking for errors.
			Gl.GetProgram(Shader, GLEnum.LinkStatus, out var status);
			if (status == 0)
			{
				Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(Shader)}");
			}

			//Delete the no longer useful individual shaders;
			Gl.DetachShader(Shader, vertexShader);
			Gl.DetachShader(Shader, fragmentShader);
			Gl.DeleteShader(vertexShader);
			Gl.DeleteShader(fragmentShader);

			//Tell opengl how to give the data to the shaders.
			Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), null);
			Gl.EnableVertexAttribArray(0);




			VaoLine = Gl.GenVertexArray();
			Gl.BindVertexArray(VaoLine);

			VboLine = Gl.GenBuffer(); //Creating the buffer.
			Gl.BindBuffer(BufferTargetARB.ArrayBuffer, VboLine); //Binding the buffer.

			Span<System.Numerics.Vector3> line_vbuffer = stackalloc System.Numerics.Vector3[2];
			line_vbuffer[0] = new System.Numerics.Vector3(0, 0, 0);
			line_vbuffer[1] = new System.Numerics.Vector3(10, 10, 0);


			Gl.BufferData<System.Numerics.Vector3>(BufferTargetARB.ArrayBuffer, line_vbuffer , BufferUsageARB.StaticDraw); //Setting buffer data.
			
			//Initializing a element buffer that holds the index data.
			EboLine = Gl.GenBuffer(); //Creating the buffer.
			Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, EboLine); //Binding the buffer.
			fixed (void* i = &Indices[0])
			{
				Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(2 * sizeof(uint)), i, BufferUsageARB.StaticDraw); //Setting buffer data.
			}

			Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), null);
			Gl.EnableVertexAttribArray(0);





			float aspect = (float)window.Size.X / (float)window.Size.Y;
			if (window.Size.X >= window.Size.Y)
			{

				VP = System.Numerics.Matrix4x4.CreateOrthographicOffCenter(-zoom * aspect, zoom * aspect, -zoom + pan_y, zoom + pan_y, -1.0f, 1.0f);
			}
			else
			{
				VP = System.Numerics.Matrix4x4.CreateOrthographicOffCenter(-zoom, zoom, -zoom / aspect + pan_y, zoom / aspect + pan_y, -1.0f, 1.0f);
			}
		}

		static System.Numerics.Matrix4x4 VP;


		//Uniforms are properties that applies to the entire geometry
		private static void SetColor(System.Numerics.Vector4 color)
		{
			//Setting a uniform on a shader using a name.
			int location = Gl.GetUniformLocation(Shader, "_Color");
			if (location == -1) //If GetUniformLocation returns -1 the uniform is not found.
			{
				throw new Exception("_Color uniform not found on shader.");
			}
			Gl.Uniform4(location,ref color);
		}

		private unsafe static void SetM( System.Numerics.Matrix4x4  m)
		{
			int location = Gl.GetUniformLocation(Shader, "_MVP");
			if (location == -1) //If GetUniformLocation returns -1 the uniform is not found.
			{
				throw new Exception("_MVP uniform not found on shader.");
			}
			
			var mvp = m *   VP ;

			

			Gl.UniformMatrix4(location,1, false,  (float*)&mvp );

		}

		private unsafe static void OnRender(double obj)
		{
			//Here all rendering should be done.
			Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			//Bind the geometry and shader.
			Gl.BindVertexArray(Vao);
			Gl.UseProgram(Shader);

			//绘制所有Body;
			var bodies_index = world.Type._link_codescope.Members.FindIndex(m=>m.QName.Name == "bodies");
			var bodies_v = world.ReadSlot((ushort)bodies_index, world.Type._link_codescope, Player);

			Span<System.Numerics.Vector3> line_vbuffer = stackalloc System.Numerics.Vector3[2];

			int body_count = Player.GetVectorLen(bodies_v);
			for (int i = 0; i < body_count ; i++)
			{
				var body_v = Player.GetVectorElement( bodies_v, i);

				RtInstance body = (RtInstance)Player.Context.GC.Heap[body_v.HeapPtr];

				//float rotation
				var rotation_v = body.ReadSlot((ushort)body_rotation, body.Type._link_codescope, Player);
				float rotation = rotation_v.FloatValue;

				var pos_v = body.ReadSlot((ushort)body_positoin, body.Type._link_codescope, Player);
				RtInstance pos = (RtInstance)Player.Context.GC.Heap[pos_v.HeapPtr];

				var width_v = body.ReadSlot((ushort)body_width, body.Type._link_codescope, Player);
				RtInstance width = (RtInstance)Player.Context.GC.Heap[width_v.HeapPtr];

				float pos_x = pos.ReadSlot(0,pos.Type._link_codescope,Player).FloatValue;
				float pos_y = pos.ReadSlot(1,pos.Type._link_codescope,Player).FloatValue;

				float width_x = width.ReadSlot(0, width.Type._link_codescope, Player).FloatValue;
				float width_y = width.ReadSlot(1, width.Type._link_codescope, Player).FloatValue;

				if ( hasbomb && i == body_count - 1)
				{
					SetColor(new System.Numerics.Vector4(0.4f, 0.9f, 0.4f, 1)); //0.4f, 0.9f, 0.4f
				}
				else
				{
					SetColor(new System.Numerics.Vector4(0.8f, 0.8f, 0.9f, 1));
				}
				//Draw the geometry.
				SetM(
					
					System.Numerics.Matrix4x4.CreateScale( width_x  ,width_y ,1 )
					* System.Numerics.Matrix4x4.CreateRotationZ(rotation)
					* System.Numerics.Matrix4x4.CreateTranslation(pos_x, pos_y, 0)
					);

				Gl.DrawElements(PrimitiveType.LineLoop, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);

			}


			Gl.BindVertexArray(VaoLine);
			//joints
			var joints_index = world.Type._link_codescope.Members.FindIndex(m => m.QName.Name == "joints");
			var joints_v = world.ReadSlot((ushort)joints_index, world.Type._link_codescope, Player);
			for (int i = 0; i < Player.GetVectorLen(joints_v); i++)
			{
				var joint_v = Player.GetVectorElement(joints_v, i);
				RtInstance joint = (RtInstance)Player.Context.GC.Heap[joint_v.HeapPtr];

				var body1_v = joint.ReadSlot((ushort)joint_body1, joint.Type._link_codescope, Player);
				RtInstance body1 = (RtInstance)Player.Context.GC.Heap[body1_v.HeapPtr];

				//float rotation
				var rotation1_v = body1.ReadSlot((ushort)body_rotation, body1.Type._link_codescope, Player);
				float rotation1 = rotation1_v.FloatValue;
				var pos1_v = body1.ReadSlot((ushort)body_positoin, body1.Type._link_codescope, Player);
				RtInstance pos1 = (RtInstance)Player.Context.GC.Heap[pos1_v.HeapPtr];

				var body2_v = joint.ReadSlot((ushort)joint_body2, joint.Type._link_codescope, Player);
				RtInstance body2 = (RtInstance)Player.Context.GC.Heap[body2_v.HeapPtr];

				//float rotation
				var rotation2_v = body2.ReadSlot((ushort)body_rotation, body2.Type._link_codescope, Player);
				float rotation2 = rotation2_v.FloatValue;
				var pos2_v = body2.ReadSlot((ushort)body_positoin, body2.Type._link_codescope, Player);
				RtInstance pos2 = (RtInstance)Player.Context.GC.Heap[pos2_v.HeapPtr];

				var localAnchor1_v = joint.ReadSlot((ushort) joint_localAnchor1, joint.Type._link_codescope, Player);
				RtInstance localAnchor1 = (RtInstance)Player.Context.GC.Heap[localAnchor1_v.HeapPtr];

				var localAnchor2_v = joint.ReadSlot((ushort)joint_localAnchor2, joint.Type._link_codescope, Player);
				RtInstance localAnchor2 = (RtInstance)Player.Context.GC.Heap[localAnchor2_v.HeapPtr];

				//Mat22 R1(b1->rotation);
				//Mat22 R2(b2->rotation);

				//Vec2 x1 = b1->position;
				//Vec2 p1 = x1 + R1 * joint->localAnchor1;

				//Vec2 x2 = b2->position;
				//Vec2 p2 = x2 + R2 * joint->localAnchor2;

				System.Numerics.Vector2 x1 = 
					new System.Numerics.Vector2(pos1.ReadSlot(0, pos1.Type._link_codescope, Player).FloatValue,
					pos1.ReadSlot(1, pos1.Type._link_codescope, Player).FloatValue
					);


				float la1_x = localAnchor1.ReadSlot(0, localAnchor1.Type._link_codescope, Player).FloatValue;
				float la1_y = localAnchor1.ReadSlot(1, localAnchor1.Type._link_codescope, Player).FloatValue;

				var p1 = x1 + new System.Numerics.Vector2( la1_x * MathF.Cos(rotation1) - la1_y * MathF.Sin(rotation1),la1_x * MathF.Sin(rotation1) + la1_y * MathF.Cos(rotation1) );

				System.Numerics.Vector2 x2 =
					new System.Numerics.Vector2(pos2.ReadSlot(0, pos2.Type._link_codescope, Player).FloatValue,
					pos2.ReadSlot(1, pos2.Type._link_codescope, Player).FloatValue
					);
				
				float la2_x = localAnchor2.ReadSlot(0, localAnchor2.Type._link_codescope, Player).FloatValue;
				float la2_y = localAnchor2.ReadSlot(1, localAnchor2.Type._link_codescope, Player).FloatValue;

				var p2 = x2 + new System.Numerics.Vector2(la2_x * MathF.Cos(rotation2) - la2_y * MathF.Sin(rotation2), 
					la2_x * MathF.Sin(rotation2) + la2_y * MathF.Cos(rotation2));
				
				SetColor(new System.Numerics.Vector4(0.5f, 0.5f, 0.8f, 1));
				SetM(System.Numerics.Matrix4x4.Identity);


				line_vbuffer[0] = new System.Numerics.Vector3(x1.X, x1.Y, 0);
				line_vbuffer[1] = new System.Numerics.Vector3(p1.X, p1.Y, 0);
				Gl.BufferData<System.Numerics.Vector3>(BufferTargetARB.ArrayBuffer, line_vbuffer, BufferUsageARB.StaticDraw); //Setting buffer data.

				Gl.DrawElements(PrimitiveType.Lines, 2, DrawElementsType.UnsignedInt, null);



				line_vbuffer[0] = new System.Numerics.Vector3(x2.X, x2.Y, 0);
				line_vbuffer[1] = new System.Numerics.Vector3(p2.X, p2.Y, 0);
				Gl.BufferData<System.Numerics.Vector3>(BufferTargetARB.ArrayBuffer, line_vbuffer, BufferUsageARB.StaticDraw); //Setting buffer data.

				Gl.DrawElements(PrimitiveType.Lines, 2, DrawElementsType.UnsignedInt, null);

			}


			//arbiters
			var arbiters_index = world.Type._link_codescope.Members.FindIndex(m => m.QName.Name == "arbiters");
			var arbiters_v = world.ReadSlot((ushort)arbiters_index, world.Type._link_codescope, Player);
			Gl.BindVertexArray(Vao);
			Gl.PointSize(4);

			int alen = Player.GetVectorLen(arbiters_v);

			for (int i = 0; i < alen; i++)
			{
				var arbiter_v = Player.GetVectorElement(arbiters_v, i);
				RtInstance arbiter = (RtInstance)Player.Context.GC.Heap[arbiter_v.HeapPtr];

				int num = arbiter.ReadSlot((ushort)arbiter_numContacts, arbiter.Type._link_codescope, Player).IntValue;

				var contacts = arbiter.ReadSlot((ushort)arbiter_contacts, arbiter.Type._link_codescope, Player);

				for (int j = 0; j < num; j++)
				{
					var contact_v = Player.GetVectorElement(contacts, j);
					RtInstance contact = (RtInstance)Player.Context.GC.Heap[contact_v.HeapPtr];

					var pos_v = contact.ReadSlot(0, contact.Type._link_codescope, Player);
					RtInstance pos = (RtInstance)Player.Context.GC.Heap[pos_v.HeapPtr];

					float pos_x = pos.ReadSlot(0, pos.Type._link_codescope, Player).FloatValue;
					float pos_y = pos.ReadSlot(1, pos.Type._link_codescope, Player).FloatValue;

					SetColor(new System.Numerics.Vector4(1.0f, 0.0f, 0.0f, 1));

					SetM(

						System.Numerics.Matrix4x4.CreateTranslation(pos_x - 0.5f, pos_y - 0.5f, 0)
					);

					Gl.DrawElements(PrimitiveType.Points, 1, DrawElementsType.UnsignedInt, null);
				}


			}

			Gl.PointSize(1);
		}

		private static void OnUpdate(double obj)
		{
			//Here all updates to the program should be done.

			Player.InvokeStaticMethod(Step);

		}

		private static void OnFramebufferResize(Vector2D<int> newSize)
		{
			//Update aspect ratios, clipping regions, viewports, etc.

			Gl.Viewport(newSize);

			float aspect = (float)newSize.X / (float)newSize.Y;
			if (newSize.X >= newSize.Y)
			{

				VP = System.Numerics.Matrix4x4.CreateOrthographicOffCenter(-zoom * aspect, zoom * aspect, -zoom + pan_y, zoom + pan_y, -1.0f, 1.0f);
			}
			else
			{
				VP = System.Numerics.Matrix4x4.CreateOrthographicOffCenter(-zoom, zoom, -zoom / aspect + pan_y, zoom / aspect + pan_y, -1.0f, 1.0f);
			}
		}
		private static bool hasbomb = false;
		private static void KeyDown(IKeyboard arg1, Key arg2, int arg3)
		{
			//Check to close the window on escape.
			if (arg2 == Key.Escape)
			{
				window.Close();
			}

			if (arg2 == Key.Space)
			{
				var main = Player.Context.libs.SelectMany(l => l.Classes).First(c => c != null && c.QName.Name == "Main");
				var bomb = main.Traits.First(t => t.QName.Name == "LaunchBomb").Method;

				Player.InvokeStaticMethod(bomb);

				hasbomb = true;
			}
			else if (arg2 == Key.Number1)
			{
				var main = Player.Context.libs.SelectMany(l => l.Classes).First(c => c != null && c.QName.Name == "Main");
				var demo = main.Traits.First(t => t.QName.Name == "Demo1").Method;

				Player.InvokeStaticMethod(demo);

				window.Title = "A Single Box";
				hasbomb = false;
			}
			else if (arg2 == Key.Number2)
			{
				var main = Player.Context.libs.SelectMany(l => l.Classes).First(c => c != null && c.QName.Name == "Main");
				var demo = main.Traits.First(t => t.QName.Name == "Demo2").Method;

				Player.InvokeStaticMethod(demo);

				window.Title = "A simple pendulum";
				hasbomb = false;
			}
			else if (arg2 == Key.Number3)
			{
				var main = Player.Context.libs.SelectMany(l => l.Classes).First(c => c != null && c.QName.Name == "Main");
				var demo = main.Traits.First(t => t.QName.Name == "Demo3").Method;

				Player.InvokeStaticMethod(demo);

				window.Title = "Varying friction coefficients";
				hasbomb = false;
			}
			else if (arg2 == Key.Number4)
			{
				var main = Player.Context.libs.SelectMany(l => l.Classes).First(c => c != null && c.QName.Name == "Main");
				var demo = main.Traits.First(t => t.QName.Name == "Demo4").Method;

				Player.InvokeStaticMethod(demo);

				window.Title = "A vertical stack";
				hasbomb = false;
			}
			else if (arg2 == Key.Number5)
			{
				var main = Player.Context.libs.SelectMany(l => l.Classes).First(c => c != null && c.QName.Name == "Main");
				var demo = main.Traits.First(t => t.QName.Name == "Demo5").Method;

				Player.InvokeStaticMethod(demo);

				window.Title = "A pyramid";
				hasbomb = false;
			}
			else if (arg2 == Key.Number6)
			{
				var main = Player.Context.libs.SelectMany(l => l.Classes).First(c => c != null && c.QName.Name == "Main");
				var demo = main.Traits.First(t => t.QName.Name == "Demo6").Method;

				Player.InvokeStaticMethod(demo);

				window.Title = "A teeter";
				hasbomb = false;
			}
			else if (arg2 == Key.Number7)
			{
				var main = Player.Context.libs.SelectMany(l => l.Classes).First(c => c != null && c.QName.Name == "Main");
				var demo = main.Traits.First(t => t.QName.Name == "Demo7").Method;

				Player.InvokeStaticMethod(demo);

				window.Title = "A suspension bridge";
				hasbomb = false;
			}
			else if (arg2 == Key.Number8)
			{
				var main = Player.Context.libs.SelectMany(l => l.Classes).First(c => c != null && c.QName.Name == "Main");
				var demo = main.Traits.First(t => t.QName.Name == "Demo8").Method;

				Player.InvokeStaticMethod(demo);

				window.Title = "Dominos";
				hasbomb = false;
			}
		}


	}
}
