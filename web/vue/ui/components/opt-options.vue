<template>
  <div>
    <span>地图设置:</span>
    <el-row v-for="(v,k) in mapSettings" :key="k">
      <el-col>
        <el-button icon="el-icon-loading" circle class="btn-i"></el-button>
      </el-col>
      <el-col>{{k}}</el-col>
      <el-col>{{v}}</el-col>
      <el-col>操作</el-col>
    </el-row>
  </div>
</template>

<script>
import ElRow from "element-ui/lib/row";
import ElButton from "element-ui/lib/button";

const components = { ElRow, ElButton };
export default {
  components,
  data() {
    return {
      mapSettings: {
        canvasSize: { w: 500, h: 617 },
        mapSeed: [972402960, 1, 999999999],
        pointsNumber: [10, 10, 100],
        mapName: "Persainy",
        yearAndEra: { y: 166, e: "Helbo Era" },
        mapTemplate: "Island",
        culturesNumber: [11, 1, 32],
        culturesSet: "All-world",
        statesNumber: [14, 0, 99],
        provincesRatio: [20, 0, 100],
        sizeVariety: [2.93, 0, 10],
        growthRate: [1.4, 0.1, 2],
        townsNumber: [1000, 0, 1000],
        religionsNumber: [4, 0, 50],
      },
      generatorSettings: {
        onloadBehavior: 0,
        interfaceSize: [1, 0.6, 3],
        tooltipSize: [14, 4, 32],
        transparency: [5, 0, 100],
        zoomExtent: { min: 1, max: 20 },
      },
    };
  },
};
</script>

<style scoped>
.btn-i {
  border: none;
}
</style>