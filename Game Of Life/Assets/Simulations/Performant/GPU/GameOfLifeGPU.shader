// Original source:
// https://github.com/sevelee/2d-game-of-life-by-frag-shader/blob/master/Assets/2DGameOfLife/Shaders/Cell%20Shader.shader
Shader "Custom/GameOfLife" {
	Properties {
		_MainTex ("Current State", 2D) = "white" {}
		_Cells("Number of Cells in one side", Range(1, 2000)) = 100
	}
	SubShader {
		Pass {
			CGPROGRAM
			
			#pragma vertex vert_img
			#pragma fragment frag

			#include "UnityCG.cginc"

			uniform sampler2D _MainTex;
			uniform float _Cells;

			static int cellsNewState_isAlive[9] = { 0, 0, 1, 1, 0, 0, 0, 0, 0 };
			static int cellsNewState_isDead[9] = { 0, 0, 0, 1, 0, 0, 0, 0, 0 };

			float4 frag(v2f_img i) : COLOR
			{
				float2 uv = i.uv;
				float width = 1 / _Cells;

				//1 2 3
				//4 5 6
				//7 8 9
				const float cell1 = tex2D(_MainTex, uv + float2(-width, -width)).r;
				const float cell2 = tex2D(_MainTex, uv + float2(     0, -width)).r;
				const float cell3 = tex2D(_MainTex, uv + float2(+width, -width)).r;
				const float cell4 = tex2D(_MainTex, uv + float2(-width,      0)).r;
				const float cell6 = tex2D(_MainTex, uv + float2(+width,      0)).r;
				const float cell7 = tex2D(_MainTex, uv + float2(-width, +width)).r;
				const float cell8 = tex2D(_MainTex, uv + float2(     0, +width)).r;
				const float cell9 = tex2D(_MainTex, uv + float2(+width, +width)).r;

				int neighbors = cell1 + cell2 + cell3 + cell4 + cell6 + cell7 + cell8 + cell9;

				if (tex2D(_MainTex, uv).r == 1)
					return float4(cellsNewState_isAlive[neighbors], cellsNewState_isAlive[neighbors], cellsNewState_isAlive[neighbors], 1);
				else
					return float4(cellsNewState_isDead[neighbors], cellsNewState_isDead[neighbors], cellsNewState_isDead[neighbors], 1);
			}
			ENDCG
		}
	}
}
