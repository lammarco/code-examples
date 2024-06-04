using UnityEngine;
using UnityEngine.U2D;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using UnityEditor;

[EditorTool("SpriteShape Corner Rounder Tool", typeof(SpriteShapeController))]
public class SpriteShapeRounderTool : SpriteShapeTool
{
	bool lSmooth = true;
	bool rSmooth = true;
	int splineIndex;
	float curvature = CIRCLE;
	Rect off;
	public override string displayName => "SpriteShape Corner Rounder Tool";
	public override int selectedIndex {
		get{return splineIndex;} 
		set{splineIndex = SplineClamp(value);}
	}
	
	public override void OnGUI(){
		curvature = base.CurveGUI(curvature);
		splineIndex = base.IndexGUI(splineIndex, pointCount);
		
		if (GUILayout.Button("Set Tangents to Linear")){
			Undo.RecordObject(target, 
				"Linearize Tangents: " + splineIndex.ToString());
				SetLinearTangents();
		}
		EditorGUILayout.Space();
		
		
		off = EditorGUILayout.BeginHorizontal();
		GUILayout.Label("LSmooth");
		lSmooth = EditorGUILayout.Toggle(lSmooth,GUILayout.MaxWidth(20));
		rSmooth = EditorGUILayout.Toggle(rSmooth,GUILayout.MaxWidth(20));
		GUILayout.Label("RSmooth");
		EditorGUILayout.EndHorizontal();
		if (GUILayout.Button("Apply Corner")){
			Undo.RecordObject(ssc, $"Round Corner: {ssc.name} {splineIndex.ToString()}");
			ApplyCorner();
		}
		
	}
	
	public void ApplyCorner(){
		//round the corner
		int prev, next;
		GetNeighbors(out prev, out next);
		
		Vector3 prevPos, nextPos, currentPos;
		Vector3 startTangent, endTangent;
		GetPositions(prev, next, out currentPos, out prevPos, out nextPos);
		GetTangents(currentPos, prevPos, nextPos, out startTangent, out endTangent);
		
		RoundCorner(prev,next,prevPos,nextPos,currentPos);
		
		//automatically move to the next points
		splineIndex = this.IndexLoop(splineIndex+1, pointCount);
		base.RefreshShape(ssc);
	}
	
	public void RoundCorner(int prev, int next,
	  Vector3 prevPos, Vector3 nextPos, Vector3 currentPos){
		spline.SetTangentMode(prev, (lSmooth) 
			? ShapeTangentMode.Continuous 
			: ShapeTangentMode.Broken);
		spline.SetTangentMode(next, (rSmooth) 
			? ShapeTangentMode.Continuous 
			: ShapeTangentMode.Broken);
		if(lSmooth) spline.SetLeftTangent(prev, (prevPos-currentPos)*curvature);
		if(rSmooth) spline.SetRightTangent(next, (nextPos-currentPos)*curvature);
		
		//draw the tangents for the curve
		spline.SetRightTangent(prev, (currentPos-prevPos)*curvature);
		spline.SetLeftTangent(next,  (currentPos-nextPos)*curvature);
		spline.RemovePointAt(splineIndex);
	}
	
	public void GetNeighbors(out int prev,out int next){
		prev = this.IndexLoop(splineIndex-1, pointCount);
		next = this.IndexLoop(splineIndex+1, pointCount);
	}
	
	public void GetPositions(int prev, int next, 
		out Vector3 currentPos, out Vector3 prevPos, out Vector3 nextPos){
		currentPos = spline.GetPosition(splineIndex);
		prevPos = spline.GetPosition(prev);
		nextPos = spline.GetPosition(next);
	}
	
	public void GetTangents(Vector3 currentPos, Vector3 prevPos, Vector3 nextPos, 
		out Vector3 startTangent, out Vector3 endTangent){
		startTangent = (currentPos-prevPos)*curvature;
		endTangent = (currentPos-nextPos)*curvature;
	}
	
	public void SetLinearTangents(){
		int prev, next;
		GetNeighbors(out prev, out next);
		
		spline.SetTangentMode(prev, ShapeTangentMode.Linear);
		spline.SetTangentMode(splineIndex, ShapeTangentMode.Linear);
		spline.SetTangentMode(next, ShapeTangentMode.Linear);
	}

    // Equivalent to Editor.OnSceneGUI.
    public override void OnToolGUI(EditorWindow window)
    {
        if (!(window is SceneView sceneView) || (ssc is null))
            return;
		
		int prev, next;
		Vector3 prevPos, nextPos, currentPos;
		Vector3 startTangent,endTangent;
			
		GetNeighbors(out prev, out next);
		GetPositions(prev, next,
			out currentPos, out prevPos, out nextPos);
		GetTangents(currentPos, prevPos, nextPos, 
			out startTangent, out endTangent);
		currentPos += posOffset;
		nextPos += posOffset;
		prevPos += posOffset;
			
		EditorGUI.BeginChangeCheck();
		Handles.color = Color.red;
		Handles.DrawWireDisc(prevPos,Vector3.forward,0.2f);
		Handles.DrawWireDisc(nextPos,Vector3.forward,0.2f);
				
		Handles.color = Color.red;
		Handles.DrawWireDisc(currentPos,Vector3.forward,0.5f);
			
		Handles.DrawLine(prevPos-(startTangent/2), prevPos+startTangent);
		Handles.DrawLine(nextPos-(endTangent/2), nextPos+endTangent);
			
		Handles.color = Color.green;
		Handles.DrawBezier(prevPos, nextPos, 
			prevPos+startTangent, nextPos+endTangent, 
			Color.green, null, 2);
		if (EditorGUI.EndChangeCheck()){}
	}
}