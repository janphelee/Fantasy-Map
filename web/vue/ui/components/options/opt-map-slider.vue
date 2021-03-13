<template>
  <el-row type="flex"
          justify="start"
          align="middle">
    <el-col :xs="7"
            :sm="5"
            :md="4"
            :lg="3"
            :xl="2">
      <el-button :icon="lock?'el-icon-lock': 'el-icon-unlock'"
                 circle
                 class="btn-i"
                 @click="lock=!lock"></el-button>
      <span @click="lock=!lock">{{label}}:</span>
    </el-col>
    <el-col :xs="17"
            :sm="14"
            :md="11"
            :lg="9"
            :xl="8">
      <el-slider v-model="current"
                 input-size="mini"
                 :min="min"
                 :max="max"
                 :step="step"
                 :disabled="lock"
                 :show-input="true"
                 :show-input-controls="true"></el-slider>
    </el-col>
  </el-row>
</template>

<script>
import ElRow from "element-ui/lib/row";
import ElCol from "element-ui/lib/col";
import ElSlider from "element-ui/lib/slider";
import ElButton from "element-ui/lib/button";

const components = {ElRow, ElCol, ElSlider, ElButton};

export default {
  components,
  model: {
    prop: 'value',
    event: 'input'
  },
  props: {
    label: String,
    value: Number,
    min: Number,
    max: Number,
    step: Number,
  },
  data () {
    return {
      lock: false,
      current: Number,
    }
  },
  watch: {
    value (v) {
      this.current = v;
    },
    current (v) {
      this.$emit('input', v);
    },
  },
}
</script>

<style scoped>
.btn-i {
  border: none;
}

.input-width-min {
  width: 272px;
}
</style>
